using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;

using HostBox.Borderline;

namespace HostShim
{
    /// <summary>
    /// Загрузчик, который устанавливается в домен приложения и выполняет загрузку компонентов приложения. 
    /// </summary>
    public class MefDomainComponent : MarshalByRefObject, IComponent
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MefDomainComponent"/>. 
        /// </summary>
        /// <param name="componentPath">Путь к исполняемым модулям компонента.</param>
        public MefDomainComponent(string componentPath)
        {
            this.Path = componentPath;
        }

        /// <summary>
        /// Путь к исполняемому коду компонента.
        /// </summary>
        public string Path { get; private set; }

        [ImportMany(typeof(IHostableComponentFactory))]
        // ReSharper disable once UnusedAutoPropertyAccessor.Local Инициализируется через MEF.
        private IEnumerable<IHostableComponentFactory> HostableComponentFactories { get; set; }

        private IEnumerable<IHostableComponent> HostableComponent { get; set; }

        /// <summary>
        /// Запускает компонент.
        /// </summary>
        public void Start()
        {
            Directory.SetCurrentDirectory(Path);
            TaskScheduler.UnobservedTaskException += this.UnobservedTaskException;

            this.LoadComponentFactories();
            this.CreateComponents();
            this.StartComponents();
        }

        /// <summary>
        /// Возобновляет работу загружаемого компонента.
        /// </summary>
        public void Resume()
        {
            foreach (var hostableComponent in this.HostableComponent)
            {
                hostableComponent.Resume();
            }
        }

        /// <summary>
        /// Приостанавливает работу загружаемого компонента.
        /// </summary>
        public void Pause()
        {
            foreach (var hostableComponent in this.HostableComponent)
            {
                hostableComponent.Pause();
            }
        }

        /// <summary>
        /// Останавливает работу загружаемого компонента.
        /// </summary>
        public void Stop()
        {
            foreach (var hostableComponent in this.HostableComponent)
            {
                hostableComponent.Stop();
            }
        }

        /// <inheritdoc />
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null;
        }

        private void StartComponents()
        {
            foreach (var hostableComponent in this.HostableComponent)
            {
                hostableComponent.Start();
            }
        }

        private void CreateComponents()
        {
            if (this.HostableComponentFactories == null)
            {
                this.HostableComponent = Enumerable.Empty<IHostableComponent>();
            }
            else
            {
                List<IHostableComponentFactory> factories = this.HostableComponentFactories.ToList();

                var components = factories.Select(hostableComponentFactory => hostableComponentFactory.CreateComponent()).ToList();

                this.HostableComponent = components;
            }
        }

        private void LoadComponentFactories()
        {
            var catalog = new DirectoryCatalog(this.Path);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        private void UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
        }
    }
}
