namespace Hangfire.Console.Storage
{
    internal interface IOperationStream
    {
        void Write(Operation operation);

        void Flush();
    }
}