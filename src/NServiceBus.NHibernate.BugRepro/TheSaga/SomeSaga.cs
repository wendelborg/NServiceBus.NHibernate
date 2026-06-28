namespace TheSaga;

using NServiceBus;

public class SomeSaga : Saga<SagaData>, IAmStartedByMessages<SagaMsg>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
    {
        mapper.MapSaga(s => s.Corr)
            .ToMessage<SagaMsg>(m => m.Corr);
    }

    public Task Handle(SagaMsg message, IMessageHandlerContext context)
    {
        MarkAsComplete();
        return Task.CompletedTask;
    }
}

public class SagaData : ContainSagaData
{
    public virtual string Corr { get; set; }
}

public class SagaMsg : ICommand
{
    public string Corr { get; set; }
}
