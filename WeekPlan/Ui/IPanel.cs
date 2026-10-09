namespace WeekPlan.App.Ui;

public interface IPanel<T>
{
    bool IsOpen { get; }
    T? Data { get; }
    void Open(T data);
    void Refresh(T data);
    void Close();
}
