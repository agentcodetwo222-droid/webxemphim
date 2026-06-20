namespace webxemphim.Models.Schema
{
    public class AccountRow
    {
        public int AccId { get; set; }
        public string AccName { get; set; } = "";
        public string AccHash { get; set; } = "";
        public string AccEmail { get; set; } = "";
        public string AccRole { get; set; } = "User";
        public bool AccLocked { get; set; }
        public int AccStamp { get; set; }
    }

    public class AccountDateRow
    {
        public int AccdId { get; set; }
        public int AccdAcc { get; set; }
        public DateTime AccdCreated { get; set; }
    }

    public class ProfileRow
    {
        public int ProfId { get; set; }
        public int ProfAcc { get; set; }
        public string ProfName { get; set; } = "";
        public string? ProfPhone { get; set; }
        public string? ProfAddress { get; set; }
        public string? ProfAvatar { get; set; }
    }

    public class ProfileDateRow
    {
        public int ProfdId { get; set; }
        public int ProfdProf { get; set; }
        public DateTime? ProfdVipExp { get; set; }
        public DateTime ProfdUpdated { get; set; }
    }

    public class WalletRow
    {
        public int WalId { get; set; }
        public int WalAcc { get; set; }
        public string WalName { get; set; } = "";
        public string WalBalance { get; set; } = "";
    }

    public class WalletDateRow
    {
        public int WaldId { get; set; }
        public int WaldWal { get; set; }
        public DateTime WaldUpdated { get; set; }
    }

    public class MovieInfoRow
    {
        public int MovId { get; set; }
        public string MovTitle { get; set; } = "";
        public string MovDesc { get; set; } = "";
        public string MovGenre { get; set; } = "";
        public string MovCountry { get; set; } = "";
        public string MovYear { get; set; } = "";
        public int? MovDuration { get; set; }
        public string? MovActors { get; set; }
        public string? MovDirector { get; set; }
        public string MovCategory { get; set; } = "";
        public bool MovVip { get; set; }
        public bool MovActive { get; set; }
        public int MovViews { get; set; }
    }

    public class MovieDateRow
    {
        public int MovdId { get; set; }
        public int MovdMov { get; set; }
        public DateTime MovdCreated { get; set; }
    }

    public class MovieMediaRow
    {
        public int MediaId { get; set; }
        public int MediaMov { get; set; }
        public string MediaTitle { get; set; } = "";
        public string MediaImage { get; set; } = "";
        public string MediaVideo { get; set; } = "";
    }

    public class MediaDateRow
    {
        public int MediadId { get; set; }
        public int MediadMedia { get; set; }
        public DateTime MediadUpdated { get; set; }
    }

    public class TxHeaderRow
    {
        public int TxId { get; set; }
        public int TxAcc { get; set; }
        public string TxName { get; set; } = "";
        public string TxType { get; set; } = "";
        public string TxDesc { get; set; } = "";
        public string TxStatus { get; set; } = "Pending";
    }

    public class TxDateRow
    {
        public int TxdateId { get; set; }
        public int TxdateTx { get; set; }
        public DateTime TxdateCreated { get; set; }
    }

    public class TxDetailRow
    {
        public int TxdId { get; set; }
        public int TxdTx { get; set; }
        public string TxdAmount { get; set; } = "";
        public string TxdCurrency { get; set; } = "";
        public string TxdVnd { get; set; } = "";
    }

    public class BillHeaderRow
    {
        public int BillId { get; set; }
        public string BillCode { get; set; } = "";
        public int BillAcc { get; set; }
        public string BillName { get; set; } = "";
        public string BillEmail { get; set; } = "";
        public string BillTx { get; set; } = "";
        public string BillType { get; set; } = "";
        public string BillService { get; set; } = "";
        public string BillStatus { get; set; } = "Completed";
        public string BillNote { get; set; } = "";
    }

    public class BillDateRow
    {
        public int BilldId { get; set; }
        public int BilldBill { get; set; }
        public DateTime BilldCreated { get; set; }
    }

    public class BillDetailRow
    {
        public int BildId { get; set; }
        public int BildBill { get; set; }
        public string BildAmount { get; set; } = "";
        public string BildCurrency { get; set; } = "";
        public string BildVnd { get; set; } = "";
        public string BildBefore { get; set; } = "";
        public string BildAfter { get; set; } = "";
    }

    public class CurrencyDateRow
    {
        public int CurdId { get; set; }
        public int CurdCur { get; set; }
        public DateTime CurdUpdated { get; set; }
    }

    public class FavoriteDateRow
    {
        public int FavdId { get; set; }
        public int FavdFav { get; set; }
        public DateTime FavdAdded { get; set; }
    }

    public class WatchDateRow
    {
        public int WhdId { get; set; }
        public int WhdWh { get; set; }
        public DateTime WhdAt { get; set; }
    }

    public class AuditDateRow
    {
        public long LogdId { get; set; }
        public long LogdLog { get; set; }
        public DateTime LogdAt { get; set; }
    }

    public class LoginDateRow
    {
        public int LadId { get; set; }
        public int LadLa { get; set; }
        public DateTime LadLast { get; set; }
        public DateTime? LadUntil { get; set; }
    }
}
