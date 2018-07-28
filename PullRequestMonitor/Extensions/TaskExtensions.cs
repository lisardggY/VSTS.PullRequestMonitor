using System.Collections.Generic;
using System.Threading.Tasks;

namespace PullRequestMonitor.Extensions
{
    public static class TaskExtensions
    {
        // Converts a collection of Task<T> to a Task<T[]>
        public static async Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> source) 
            => await Task.WhenAll(source);

    }
}
