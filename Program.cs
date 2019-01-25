
namespace CommentRemover
{
    class Program
    {
        static void Main(string[] args)
        {
            var cr = new CommentRemover();
            foreach (var arg in args)
            {
                cr.Remove(arg);
            }
        }
    }
}
