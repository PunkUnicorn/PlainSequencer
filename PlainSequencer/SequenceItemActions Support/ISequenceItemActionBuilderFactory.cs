namespace PlainSequencer.SequenceItemSupport
{
    public interface ISequenceItemActionBuilderFactory
    {
        ISequenceItemActionBuilder ResolveSequenceItemActionBuilder();
    }
}
