using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScreenSystem
{
    public static class CollectionPool
    {
        private static readonly Stack<List<Task>> TaskListPool = new();

        private const int POOL_SIZE = 8;

        public static List<Task> RentTaskList()
        {
            return TaskListPool.Count > 0
                ? TaskListPool.Pop()
                : new List<Task>(8);
        }

        public static void ReturnTaskList(List<Task> list)
        {
            if (list == null)
                return;

            list.Clear();
            if (TaskListPool.Count < POOL_SIZE)
                TaskListPool.Push(list);
        }
    }
}

