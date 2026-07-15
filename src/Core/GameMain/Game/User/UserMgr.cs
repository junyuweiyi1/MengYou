public class UserMgr
{
    public long ID { get; private set; }
    public string 名字 { get; private set; }
    public string 称谓 { get; private set; }
    public string 帮派 { get; private set; }
    public long 帮贡 { get; private set; }
    public string 门派 { get; private set; }
    public long 门贡 { get; private set; }
    public int Hp { get; private set; }
    public int MaxHp { get; private set; }
    public int Mp { get; private set; }
    public int MaxMp { get; private set; }
    public int Level { get; private set; }
}