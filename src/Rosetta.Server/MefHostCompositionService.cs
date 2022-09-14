namespace Rosetta.Server
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.ComponentModel.Composition.Primitives;
    using System.Reflection;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    internal sealed partial class MefHostCompositionService : IDisposable
    {
        private readonly CompositionContainer compositionContainer;
        private readonly CompositionBatch compositionBatch;

        private bool isComposed;
        private bool isDisposed;

        [Export]
        private JoinableTaskContext joinableTaskContext = new JoinableTaskContext();

        public MefHostCompositionService(bool composeImmediately = true)
        {
            Assembly serviceAssembly = Assembly.GetExecutingAssembly();

            AggregateCatalog compositionCatalog = new AggregateCatalog();
            compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(IContentType).Assembly));    // Microsoft.VisualStudio.CoreUtility.dll
            compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ITextBuffer).Assembly));     // Microsoft.VisualStudio.Text.Data.dll
            compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(IEditorOptions).Assembly));  // Microsoft.VisualStudio.Text.Logic.dll
            compositionCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Microsoft.VisualStudio.Text.Utilities.IEncodingDetectorMetadata).Assembly));      // Microsoft.VisualStudio.Text.Core.Implementation.dll
            compositionCatalog.Catalogs.Add(new AssemblyCatalog(serviceAssembly));

            // The composition is going to be accessed from multiple threads so require it to be thread safe, see https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1231274
            this.compositionContainer = new CompositionContainer(compositionCatalog, isThreadSafe: true);
            this.compositionBatch = new CompositionBatch();
            this.compositionBatch.AddPart(this);

            if (composeImmediately)
            {
                this.Compose();
            }
        }

        public void Compose()
        {
            this.ThrowIfDisposed();

            if (!this.isComposed)
            {
                this.compositionContainer.Compose(this.compositionBatch);
                this.isComposed = true;
            }
        }

        #region ICompositionService interface
        public void SatisfyImportsOnce(ComposablePart part)
        {
            this.compositionContainer.SatisfyImportsOnce(part);
        }
        #endregion

        #region IMefHostExportProvider interface
        public TExport? GetExportedValue<TExport>()
        {
            this.ThrowIfNotComposed();
            return this.compositionContainer.GetExportedValueOrDefault<TExport>();
        }

        public Lazy<TExport>? GetExport<TExport>()
        {
            this.ThrowIfNotComposed();
            return this.compositionContainer.GetExport<TExport>();
        }

        public Lazy<TExport, TMetadata>? GetExport<TExport, TMetadata>()
        {
            this.ThrowIfNotComposed();
            return this.compositionContainer.GetExport<TExport, TMetadata>();
        }

        public IEnumerable<TExport> GetExportedValues<TExport>()
        {
            this.ThrowIfNotComposed();
            return this.compositionContainer.GetExportedValues<TExport>();
        }

        public IEnumerable<Lazy<TExport>> GetExports<TExport>()
        {
            this.ThrowIfNotComposed();
            return this.compositionContainer.GetExports<TExport>();
        }

        public IEnumerable<Lazy<TExport, TMetadata>> GetExports<TExport, TMetadata>()
        {
            this.ThrowIfNotComposed();
            return this.compositionContainer.GetExports<TExport, TMetadata>();
        }
        #endregion

        #region IDispose
        public void Dispose()
        {
            if (!this.isDisposed)
            {
                this.compositionContainer.Dispose();
                this.isDisposed = true;

                GC.SuppressFinalize(this);
            }
        }

        private void ThrowIfDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        private void ThrowIfNotComposed()
        {
            this.ThrowIfDisposed();

            if (!this.isComposed)
            {
                throw new InvalidOperationException("Must compose all MEF component parts before getting exports");
            }
        }

        #endregion
    }
}
