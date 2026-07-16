/// <summary>
/// 玩家状态快照：Reader 每次读取的结果聚合体。
/// </summary>
public sealed class UserStateSnapshot
{
    public long ID { get; private set; }
    public string 名字 { get; private set; }
    public string 称谓 { get; private set; }
    public string 帮派 { get; private set; }
    public long 帮贡 { get; private set; }
    public string 门派 { get; private set; }
    public long 门贡 { get; private set; }
    public int Hp { get; init; }
    public int MaxHp { get; init; }
    public int Mp { get; init; }
    public int MaxMp { get; init; }
    public int Level { get; init; }
}