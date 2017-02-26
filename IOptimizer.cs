using System.IO;

namespace AutoImageOptimizer
{
    interface IOptimizer
    {
        bool Optimizable(string extension);
        void Optimize(string fullPath, Stream fileStream);
    }
}
