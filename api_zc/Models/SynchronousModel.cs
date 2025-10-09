using System.Text.Json.Serialization;

namespace Accura_MES.Models
{
    #region TC (供參考)
    /// <summary>
    /// 泓記 ERP API mapping
    /// </summary>
    public class SynchronousModel
    {
        public List<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();
        public string ErrorMessage { get; set; } = string.Empty; // 錯誤訊息，如果有沒錯誤就是空的
    }

    /// <summary>
    /// 例外日
    /// </summary>
    public class ERP_ExceptionDay
    {
        public List<ERP_ExceptionDay_Data>? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty; // 錯誤訊息，如果有沒錯誤就是空的

        public bool Success { get; set; } = false;

        public override string ToString()
        {
            var dataStrings = Data != null
                ? string.Join("\n", Data.Select(d => d.ToString()))
                : "No Data";

            return $"ERP_ExceptionDay:\n" +
                   $"- ErrorMessage: {ErrorMessage}\n" +
                   $"- Data:\n{dataStrings}";
        }


        public class ERP_ExceptionDay_Data
        {
            public string Date { get; set; } = string.Empty;    // 年-月-日
            public bool Work { get; set; }                      // 是否上班

            public override string ToString()
            {
                return $"Date: {Date}, Work: {Work}";
            }
        }

    }

    /// <summary>
    /// 工序範本
    /// </summary>
    public class ERP_Process
    {
        public List<ERP_Process_Data>? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty; // 錯誤訊息，如果有沒錯誤就是空的

        public bool Success { get; set; } = false;
        public class ERP_Process_Data
        {
            public string Number { get; set; } = string.Empty;      // 作業編號 (必填)

            public string Name { get; set; } = string.Empty;       // 作業名稱 (必填)

            public string Description { get; set; } = string.Empty; // 工序描述

            public string Status { get; set; } = string.Empty;      // 工序狀態

            public string workstationNumber { get; set; } = string.Empty; // 工作站
        }
    }


    /// <summary>
    /// 工作站
    /// </summary>
    public class ERP_Workstation
    {
        public List<ERP_Workstation_Data>? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty; // 錯誤訊息，如果有沒錯誤就是空的

        public bool Success { get; set; } = false;

        public class ERP_Workstation_Data
        {
            public string Number { get; set; } = string.Empty;              // 工作站編號 (必填)

            public string Name { get; set; } = string.Empty;                // 工作站名稱 (必填)

            public int Efficiency { get; set; }                             // 效率調整

            public string WorkstationType { get; set; } = string.Empty;     // 工作站類型

            public string JobType { get; set; } = string.Empty;             // 作業型態

            public string DepartmentNumber { get; set; } = string.Empty;    // 部門編號 (必填)

            public bool IsDelete { get; set; }                              // 有效否 (必填)
        }
    }

    public class ERP_Part
    {
        public List<PartData>? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty; // 錯誤訊息，如果有沒錯誤就是空的
        public bool Success { get; set; } = false;

        public class PartData
        {
            /// <summary>
            /// 料號 (必填)
            /// </summary>
            public string Number { get; set; } = string.Empty;

            /// <summary>
            /// 料件名稱 (必填)
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 料件類型 (必填) (只有: 零件/組件/虛擬件/原物料)
            /// </summary>
            public string Type { get; set; } = string.Empty;

            /// <summary>
            /// 單位
            /// </summary>
            public string? Unit { get; set; }

            /// <summary>
            /// 規格
            /// </summary>
            public string? Specification { get; set; }

            /// <summary>
            /// 規格說明
            /// </summary>
            public string? SpecificationDes { get; set; }

            /// <summary>
            /// 資訊備註
            /// </summary>
            public string? Description { get; set; }

            /// <summary>
            /// 來源碼
            /// </summary>
            public string? SourceCode { get; set; }

            /// <summary>
            /// 分群碼
            /// </summary>
            public string? GroupCode { get; set; }

            /// <summary>
            /// 檢驗否 (boolean) (必填)
            /// </summary>
            public bool Test { get; set; }

            /// <summary>
            /// 生效 (boolean) (必填)
            /// </summary>
            public bool Enabled { get; set; }
        }

    }

    /// <summary>
    /// 線程/機器
    /// </summary>
    public class ERP_ThreadMachine
    {
        public List<ThreadMachine_Data>? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool Success { get; set; } = false;

        public class ThreadMachine_Data
        {
            public string Number { get; set; } = string.Empty; // 線別/機器編號 (必填)
            public string Name { get; set; } = string.Empty;    // 線別/機器名稱 (必填)
            public string? JobType { get; set; } // 線別/機器的作業型態
            public string? WorkstationType { get; set; } // 線別/機器的工作站類型
            public string WorkstationNumber { get; set; } = string.Empty; // 對應的工作站編號 (必填)
            public bool? isDelete { get; set; } = null;  // 線別/機器狀態 (必填)
        }
    }


    /// <summary>
    /// 工藝(製程)範本
    /// </summary>
    public class ERP_Craft
    {
        public List<Craft_Data>? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty; // 錯誤訊息，如果有沒錯誤就是空的
        public bool Success { get; set; } = false;

        public override string ToString()
        {
            var dataString = Data != null ? string.Join(", ", Data) : "null";
            return $"ERP_Craft {{ Data = [{dataString}], ErrorMessage = '{ErrorMessage}' }}";
        }

        public class Craft_Data
        {
            public string PartNumber { get; set; } = string.Empty; // 料件編號 (必填)
            public string Number { get; set; } = string.Empty; // 製程編號 (必填)
            public string Name { get; set; } = string.Empty; // 製程名稱 (必填)
            public string Description { get; set; } = string.Empty; // 製程描述
            public bool Enabled { get; set; } // 製程是否啟用 (必填)
            public string Status { get; set; } = string.Empty; // 製程狀態 (必填)
            public List<ERP_Processes>? Processes { get; set; } // 該製程所有工序

            /// <summary>
            /// 該製程所有工序
            /// </summary>
            public class ERP_Processes
            {
                public string Workflow { get; set; } = string.Empty; // 製程序 (必填)
                public string Number { get; set; } = string.Empty; // 工序編號 (必填)
                public string Workstation { get; set; } = string.Empty; // 工作站編號 (必填)
                public bool IsOut { get; set; } // 是否委外 (必填)
                public bool IsInspection { get; set; } // 是否檢驗 (必填)
                public int TMQuantity { get; set; } // 線程機器數量 (必填)

                public decimal? PlifeCycle { get; set; } // 製程單身 -> 標準人工生產時間 (不使用)

                public decimal? MlifeCycle { get; set; } // 製程單身 -> 標準機器生產時間 (不使用)

                public decimal? LifeCycle { get; set; } // 製程單身 -> 標準生產週期時間 (使用)


                public int? EmpQty { get; set; } // 最佳 (最少) 員工數量
                public List<ERP_Processes_ThreadMachine>? ThreadMachine { get; set; } // 線程機器

                public class ERP_Processes_ThreadMachine
                {
                    public string Type { get; set; } // 線程(thread) or 機器(machine),
                    public string Number { get; set; } // 線程 or 機器編號

                }
            }

        }

    }

    /// <summary>
    /// 生產訂單
    /// </summary>
    public class ERP_PO
    {
        public List<ERP_PO_Data>? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;        // 錯誤訊息，如果有沒錯誤就是空的
        public bool Success { get; set; } = false;

        public class ERP_PO_Data
        {
            public string Number { get; set; } = string.Empty;          // 工單編號 (必填)
            public string Name { get; set; } = string.Empty;            // 訂單名稱
            public DateTime StartTime { get; set; }                     // 預計開工 (年-月-日 時:分:秒) (必填)
            public DateTime? EndTime { get; set; }                      // 預計完工 (年-月-日 時:分:秒)
            public string PartNumber { get; set; } = string.Empty;      // 料件編號 (必填)
            public string CraftNumber { get; set; } = string.Empty;     // 料件編號+製程編號 (必填) [split("+") => 0:料件編號(值同上),1:製程編號]
            public string OrderQuantity { get; set; } = string.Empty;   // 生產數量 (必填)
            public bool? MoStatus { get; set; }                         // 製程否 (boolean)
            public string DepartmentNumber { get; set; } = string.Empty; // 部門編號 (非必填)

            public List<ERP_MO> Mos { get; set; } = new();             // 該工單所相關的製令

            public class ERP_MO
            {
                [JsonPropertyName("moId")]
                public string? erpMOId { get; set; }                    // ERP 製令 ID
                public string Workflow { get; set; } = string.Empty;    // 製程序 (必填)
                public string ProcessTem { get; set; } = string.Empty;  // 作業編號 (必填) [如果值為"000"代表這個是假製令，不屬於任何工作站]
                public string PartNumber { get; set; } = string.Empty;  // 料號 (必填)
                public bool IsOut { get; set; }                         // 委外 (boolean) (必填)
                public string? Workstation { get; set; }                // 工作站編號
                public bool IsInspection { get; set; }                  // 是否檢驗 (boolean) (必填)
                public List<ERP_MO_Input> Input { get; set; } = new();  // 該製令投入的料件

                public class ERP_MO_Input
                {
                    public string PartNumber { get; set; } = string.Empty;  // 料號 (必填)
                    public decimal Quantity { get; set; }                       // 數量 (必填)
                }
            }
        }
    }

    /// <summary>
    /// 拋轉 ERP 時，泓記 API 回應的物件
    /// </summary>
    public class TossResponseObject
    {
        public object? Data { get; set; }   // 暫時用不到

        public string ErrorMessage { get; set; } = string.Empty;

        public bool Success { get; set; }
    }
    #endregion TC

    #region VG
    /// <summary>
    /// MAPPING:
    /// [x2pr01] (立墩 ERP 生產工單)
    /// </summary>
    public class ERP_PO_VG
    {
        /// <summary>
        /// 工單編號
        /// </summary>
        public string Flino { get; set; } = string.Empty;
        /// <summary>
        /// 型號編號
        /// </summary>
        public string Fpzno { get; set; } = string.Empty;
        /// <summary>
        /// 型號編號說明
        /// </summary>
        public string Fpzna { get; set; } = string.Empty;
        /// <summary>
        /// 材質編號
        /// </summary>
        public string Fmzno { get; set; } = string.Empty;
        /// <summary>
        /// 加工特性
        /// </summary>
        public string F0stp { get; set; } = string.Empty;
        /// <summary>
        /// 用料
        /// </summary>
        public string Fpdn1 { get; set; } = string.Empty;
        /// <summary>
        /// 刀模編號
        /// </summary>
        public string Fcuno { get; set; } = string.Empty;
        /// <summary>
        /// 模數
        /// </summary>
        public decimal Fmoqu { get; set; }
        /// <summary>
        /// 預計用量
        /// </summary>
        public decimal Fquan { get; set; }
        /// <summary>
        /// 定容量
        /// </summary>
        public decimal Fhefc { get; set; }
        /// <summary>
        /// 該工單所相關的製令
        /// </summary>
        public List<ERP_MO_VG> Mos { get; set; } = new();
    }

    /// <summary>
    /// MAPPING:
    /// [x2pr01] (立墩 ERP 生產工單) +
    /// [x2pr01a] (立墩 ERP 製程)
    /// </summary>
    public class ERP_MO_VG
    {
        // [x2pr01]
        /// <summary>
        /// 單號
        /// </summary>
        public string Flino { get; set; } = string.Empty;
        /// <summary>
        /// 序號
        /// </summary>
        public string Fsequ { get; set; } = string.Empty;

        /// <summary>
        /// 製程編號
        /// </summary>
        public string Fstno { get; set; } = string.Empty;
        /// <summary>
        /// 製程說明
        /// </summary>
        public string Fstna { get; set; } = string.Empty;
        /// <summary>
        /// 部門編號
        /// </summary>
        public string Fdeno { get; set; } = string.Empty;
        /// <summary>
        /// 部門名稱
        /// </summary>
        public string Fdena { get; set; } = string.Empty;
    }

    /// <summary>
    /// MAPPING:
    /// [x2pr01a] (立墩 ERP 生產憑單業務訂單)
    /// </summary>
    public class ERP_PO_SO_VG
    {
        /// <summary>
        /// 單號 => [x2pr01]
        /// </summary>
        public string Flino { get; set; } = string.Empty;
        /// <summary>
        /// 業務訂單
        /// </summary>
        public string Fodno { get; set; } = string.Empty;
        /// <summary>
        /// 序號
        /// </summary>
        public string Fsequ { get; set; } = string.Empty;

    }

    /// <summary>
    /// MAPPING:
    /// [x2pr01a] (立墩 ERP 生產憑單業務訂單單身)
    /// </summary>
    public class ERP_PO_SOBODY_VG
    {
        /// <summary>
        /// 單號 => [x2pr01]
        /// </summary>
        public string Flino { get; set; } = string.Empty;
        /// <summary>
        /// 業務訂單
        /// </summary>
        public string Fodno { get; set; } = string.Empty;
        /// <summary>
        /// 序號
        /// </summary>
        public string Fsequ { get; set; } = string.Empty;
        /// <summary>
        /// 料號
        /// </summary>
        public string Fpdno { get; set; } = string.Empty;
        /// <summary>
        /// 品名
        /// </summary>
        public string Fpdna { get; set; } = string.Empty;
        /// <summary>
        /// 單位
        /// </summary>
        public string Fpdun { get; set; } = string.Empty;
        /// <summary>
        /// 數量
        /// </summary>
        public decimal Fmoqu { get; set; }
        /// <summary>
        /// 訂購數量
        /// </summary>
        public decimal Fquan { get; set; }
        /// <summary>
        /// 色數
        /// </summary>
        public string F0coq { get; set; } = string.Empty;
        /// <summary>
        /// 色數說明
        /// </summary>
        public string Fclna { get; set; } = string.Empty;
        /// <summary>
        /// 預交日期
        /// </summary>
        public DateTime? Finda { get; set; }
        /// <summary>
        /// 紙箱
        /// </summary>
        public string F0pko { get; set; } = string.Empty;
        /// <summary>
        /// 新版
        /// </summary>
        public bool F0neo { get; set; } = false;
        /// <summary>
        /// 交易別
        /// </summary>
        public string Fbano { get; set; } = string.Empty;

        /// <summary>
        /// Converts the current instance to an <see cref="ERP_PART_VG"/> object.
        /// </summary>
        /// <returns>An <see cref="ERP_PART_VG"/> object populated with the corresponding values from the current instance.</returns>
        public ERP_PART_VG ToPart()
        {
            return new ERP_PART_VG
            {
                Fpdno = Fpdno,
                Fpdna = Fpdna,
                Fpdun = Fpdun,
                F0coq = F0coq,
                Fclna = Fclna
            };
        }
    }

    /// <summary>
    /// MAPPING:
    /// [x2pr01a] (立墩 ERP 料件)
    /// </summary>
    public class ERP_PART_VG
    {
        /// <summary>
        /// 料號
        /// </summary>
        public string Fpdno { get; set; } = string.Empty;
        /// <summary>
        /// 品名
        /// </summary>
        public string Fpdna { get; set; } = string.Empty;
        /// <summary>
        /// 單位
        /// </summary>
        public string Fpdun { get; set; } = string.Empty;
        /// <summary>
        /// 色數
        /// </summary>
        public string F0coq { get; set; } = string.Empty;
        /// <summary>
        /// 色數說明
        /// </summary>
        public string Fclna { get; set; } = string.Empty;
    }

    /// <summary>
    /// MAPPING:
    /// [a0pd] (立墩 ERP 料件)
    /// </summary>
    public class ERP_BUILD_PART_VG
    {
        /// <summary>
        /// 料號(替換)
        /// </summary>
        public string Fpdno { get; set; } = string.Empty;
        /// <summary>
        /// 品名(替換)
        /// </summary>
        public string Fpdna { get; set; } = string.Empty;
        /// <summary>
        /// 單位
        /// </summary>
        public string Fpdun { get; set; } = "PCS";
        /// <summary>
        /// 編碼(替換)
        /// </summary>
        public string Funiq { get; set; } = string.Empty;
        /// <summary>
        /// 基本類別
        /// </summary>
        public string Fiono { get; set; } = "成品";
        /// <summary>
        /// 料源
        /// </summary>
        public string Fdeci { get; set; } = "自製";
        /// <summary>
        /// 材質編號
        /// </summary>
        public string Fmzno { get; set; } = "0";
        /// <summary>
        /// 材質說明
        /// </summary>
        public string Fmzna { get; set; } = "未淋模";
        /// <summary>
        /// 建立者帳號(替換)
        /// </summary>
        public string Fkeno { get; set; } = string.Empty;
        /// <summary>
        /// 建立者名稱(替換)
        /// </summary>
        public string Fkena { get; set; } = string.Empty;
        /// <summary>
        /// 建立日期(yyyy/MM/dd)預設為今天
        /// </summary>
        public string Fkeda { get; set; } = DateTime.Now.ToString("yyyy/MM/dd");
        /// <summary>
        /// 大類(替換)
        /// </summary>
        public string F0typ { get; set; } = string.Empty;

    }

    /// <summary>
    /// MAPPING:
    /// [c14k] 立墩 ERP 繳庫(需更換標記欄位)
    /// </summary>
    public class ERP_CHECKIN_VG
    {
        /// <summary>
        /// 類別代號
        /// </summary>
        public string Fiono { get; set; } = "03";
        /// <summary>
        /// 出入庫日(yyyy/MM/dd)
        /// </summary>
        public string Fdate { get; set; } = string.Empty;
        /// <summary>
        /// 交易單號(yyyyMMdd流水4碼)
        /// </summary>
        public string Flino { get; set; } = string.Empty;
        /// <summary>
        /// 單據序(XX)
        /// </summary>
        public int Fsequ { get; set; } = 0;
        /// <summary>
        /// 交易單別
        /// </summary>
        public string Fiona { get; set; } = "繳庫單";
        /// <summary>
        /// 交易類別
        /// </summary>
        public string Fbano { get; set; } = "0301";
        /// <summary>
        /// 交易名稱
        /// </summary>
        public string Fbana { get; set; } = "繳庫";
        /// <summary>
        /// 站別編號
        /// </summary>
        public string Fdeno { get; set; } = string.Empty;
        /// <summary>
        /// 站別名稱
        /// </summary>
        public string Fdena { get; set; } = string.Empty;
        /// <summary>
        /// 出入庫參數
        /// 1:雜項入庫
        /// 2.調撥出庫
        /// 3.調撥入庫
        /// </summary>
        public int Fcoso { get; set; } = 1;
        /// <summary>
        /// 日期(yyyy/MM/dd)
        /// </summary>
        public string Finda { get; set; } = string.Empty;
        /// <summary>
        /// 回報者編號(替換)
        /// </summary>
        public string Fcbo1 { get; set; } = string.Empty;
        /// <summary>
        /// 回報者名稱(替換)
        /// </summary>
        public string Fcba1 { get; set; } = string.Empty;
        /// <summary>
        /// 料號(替換)
        /// </summary>
        public string Fpdno { get; set; } = string.Empty;
        /// <summary>
        /// 品名(替換)
        /// </summary>
        public string Fpdna { get; set; } = string.Empty;
        /// <summary>
        /// 單位數量(替換)
        /// </summary>
        public double Fbaqu { get; set; } = 0;
        /// <summary>
        /// 單位總量(替換)
        /// </summary>
        public string Fpdu2 { get; set; } = string.Empty;
        /// <summary>
        /// 總量(替換)
        /// </summary>
        public double Fquan { get; set; } = 0;
        /// <summary>
        /// 總量單位(替換)
        /// </summary>
        public string Fpdun { get; set; } = string.Empty;
        /// <summary>
        /// 單位轉換權重(替換)
        /// </summary>
        public double Fpdrs { get; set; } = 1;
        /// <summary>
        /// 出入庫權重
        /// </summary>
        public int Fmopm { get; set; } = -1;
        /// <summary>
        /// 交易單編碼(替換)
        /// </summary>
        public string Funiq { get; set; } = string.Empty;
        /// <summary>
        /// 生管核准
        /// </summary>
        public string Ftrta { get; set; } = "Y";
        /// <summary>
        /// 入庫庫別(替換)
        /// </summary>
        public string Fcbo2 { get; set; } = string.Empty;
        /// <summary>
        /// 入庫庫別說明(替換)
        /// </summary>
        public string Fcba2 { get; set; } = string.Empty;
        /// <summary>
        /// 資料來源
        /// </summary>
        public string Fsour { get; set; } = "MES";
        /// <summary>
        /// 入庫日期(yyyy/MM/dd)
        /// </summary>
        public string Fcrda { get; set; } = string.Empty;
        /// <summary>
        /// 關聯表
        /// </summary>
        public string Ftbl1 { get; set; } = string.Empty;
        /// <summary>
        /// 關聯表編號
        /// </summary>
        public string Flin1 { get; set; } = string.Empty;
        /// <summary>
        /// 關聯表編碼
        /// </summary>
        public string Funi1 { get; set; } = string.Empty;
        /// <summary>
        /// 2層關聯表編號
        /// </summary>
        public string Flin2 { get; set; } = string.Empty;
        /// <summary>
        /// 2層關聯表編碼
        /// </summary>
        public string Funi2 { get; set; } = string.Empty;
        /// <summary>
        /// 報工單號?
        /// </summary>
        public string Frela { get; set; } = string.Empty;
        /// <summary>
        /// 製程代號(替換)
        /// </summary>
        public string Fstno { get; set; } = string.Empty;
        /// <summary>
        /// 製程名稱(替換)
        /// </summary>
        public string Fstna { get; set; } = string.Empty;
        /// <summary>
        /// 建立者帳號(替換)
        /// </summary>
        public string Fkeno { get; set; } = string.Empty;
        /// <summary>
        /// 建立者名稱(替換)
        /// </summary>
        public string Fkena { get; set; } = string.Empty;
        /// <summary>
        /// 建立日期(yyyy/MM/dd)
        /// </summary>
        public string Fkeda { get; set; } = string.Empty;
        /// <summary>
        /// 訂單號編碼?(替換)
        /// </summary>
        public string Funim { get; set; } = string.Empty;
        /// <summary>
        /// 價格
        /// </summary>
        public decimal Frava { get; set; } = 1;
        /// <summary>
        /// 物料批號(替換)
        /// </summary>
        public string Frano { get; set; } = string.Empty;
        /// <summary>
        /// 物料堆號(替換)
        /// </summary>
        public string Fheno { get; set; } = string.Empty;
        /// <summary>
        /// 物料堆號(替換)
        /// </summary>
        public string Fhen1 { get; set; } = string.Empty;
        /// <summary>
        /// 生管 製程代號?(替換)
        /// </summary>
        public string Fusio { get; set; } = string.Empty;
        /// <summary>
        /// 長度
        /// </summary>
        public int F0heq { get; set; } = 0;
        /// <summary>
        /// 儲位號
        /// </summary>
        public string Fslno { get; set; } = string.Empty;
        /// <summary>
        /// 儲位名稱
        /// </summary>
        public string Fslna { get; set; } = string.Empty;
    }

    /// <summary>
    /// MAPPING:
    /// [c14k] 立墩 ERP 領料(需更換標記欄位)
    /// </summary>
    public class ERP_requisition_VG
    {
        /// <summary>
        /// 類別代號
        /// </summary>
        public string Fiono { get; set; } = "54";
        /// <summary>
        /// 出入庫日(yyyy/MM/dd)
        /// </summary>
        public string Fdate { get; set; } = string.Empty;
        /// <summary>
        /// 交易單號(yyyyMMdd流水4碼)
        /// </summary>
        public string Flino { get; set; } = string.Empty;
        /// <summary>
        /// 單據序(XX)
        /// </summary>
        public int Fsequ { get; set; } = 0;
        /// <summary>
        /// 交易單別
        /// </summary>
        public string Fiona { get; set; } = "領料單";
        /// <summary>
        /// 交易類別
        /// </summary>
        public string Fbano { get; set; } = "5401";
        /// <summary>
        /// 交易名稱
        /// </summary>
        public string Fbana { get; set; } = "領料";
        /// <summary>
        /// 站別編號
        /// </summary>
        public string Fdeno { get; set; } = string.Empty;
        /// <summary>
        /// 站別名稱
        /// </summary>
        public string Fdena { get; set; } = string.Empty;
        /// <summary>
        /// 出入庫參數
        /// 1:雜項入庫
        /// 2.調撥出庫
        /// 3.調撥入庫
        /// </summary>
        public int Fcoso { get; set; } = 2;
        /// <summary>
        /// 日期(yyyy/MM/dd)
        /// </summary>
        public string Finda { get; set; } = string.Empty;
        /// <summary>
        /// 回報者編號(替換)
        /// </summary>
        public string Fcbo1 { get; set; } = string.Empty;
        /// <summary>
        /// 回報者名稱(替換)
        /// </summary>
        public string Fcba1 { get; set; } = string.Empty;
        /// <summary>
        /// 料號(替換)
        /// </summary>
        public string Fpdno { get; set; } = string.Empty;
        /// <summary>
        /// 品名(替換)
        /// </summary>
        public string Fpdna { get; set; } = string.Empty;
        /// <summary>
        /// 單位數量(替換)
        /// </summary>
        public double Fbaqu { get; set; } = 0;
        /// <summary>
        /// 單位總量(替換)
        /// </summary>
        public string Fpdu2 { get; set; } = string.Empty;
        /// <summary>
        /// 總量(替換)
        /// </summary>
        public double Fquan { get; set; } = 0;
        /// <summary>
        /// 總量單位(替換)
        /// </summary>
        public string Fpdun { get; set; } = string.Empty;
        /// <summary>
        /// 單位轉換權重(替換)
        /// </summary>
        public double Fpdrs { get; set; } = 1;
        /// <summary>
        /// 出入庫權重
        /// </summary>
        public int Fmopm { get; set; } = 1;
        /// <summary>
        /// 交易單編碼(替換)
        /// </summary>
        public string Funiq { get; set; } = string.Empty;
        /// <summary>
        /// 生管核准
        /// </summary>
        public string Ftrta { get; set; } = string.Empty;
        /// <summary>
        /// 入庫庫別(替換)
        /// </summary>
        public string Fcbo2 { get; set; } = string.Empty;
        /// <summary>
        /// 入庫庫別說明(替換)
        /// </summary>
        public string Fcba2 { get; set; } = string.Empty;
        /// <summary>
        /// 資料來源
        /// </summary>
        public string Fsour { get; set; } = "MES";
        /// <summary>
        /// 入庫日期(yyyy/MM/dd)
        /// </summary>
        public string Fcrda { get; set; } = string.Empty;
        /// <summary>
        /// 關聯表
        /// </summary>
        public string Ftbl1 { get; set; } = string.Empty;
        /// <summary>
        /// 關聯表編號
        /// </summary>
        public string Flin1 { get; set; } = string.Empty;
        /// <summary>
        /// 關聯表編碼
        /// </summary>
        public string Funi1 { get; set; } = string.Empty;
        /// <summary>
        /// 2層關聯表編號
        /// </summary>
        public string Flin2 { get; set; } = string.Empty;
        /// <summary>
        /// 2層關聯表編碼
        /// </summary>
        public string Funi2 { get; set; } = string.Empty;
        /// <summary>
        /// 報工單號?
        /// </summary>
        public string Frela { get; set; } = string.Empty;
        /// <summary>
        /// 製程代號(替換)
        /// </summary>
        public string Fstno { get; set; } = string.Empty;
        /// <summary>
        /// 製程名稱(替換)
        /// </summary>
        public string Fstna { get; set; } = string.Empty;
        /// <summary>
        /// 建立者帳號(替換)
        /// </summary>
        public string Fkeno { get; set; } = string.Empty;
        /// <summary>
        /// 建立者名稱(替換)
        /// </summary>
        public string Fkena { get; set; } = string.Empty;
        /// <summary>
        /// 建立日期(yyyy/MM/dd)
        /// </summary>
        public string Fkeda { get; set; } = string.Empty;
        /// <summary>
        /// 訂單號編碼?(替換)
        /// </summary>
        public string Funim { get; set; } = string.Empty;
        /// <summary>
        /// 價格
        /// </summary>
        public decimal Frava { get; set; } = 0;
        /// <summary>
        /// 物料批號(替換)
        /// </summary>
        public string Frano { get; set; } = string.Empty;
        /// <summary>
        /// 物料堆號(替換)
        /// </summary>
        public string Fhen1 { get; set; } = string.Empty;
        /// <summary>
        /// 物料堆號
        /// </summary>
        public string Fheno { get; set; } = string.Empty;
        /// <summary>
        /// 生管 製程代號?
        /// </summary>
        public string Fusio { get; set; } = string.Empty;
        /// <summary>
        /// 長度
        /// </summary>
        public int F0heq { get; set; } = 0;
        /// <summary>
        /// 儲位號
        /// </summary>
        public string Fslno { get; set; } = string.Empty;
        /// <summary>
        /// 儲位名稱
        /// </summary>
        public string Fslna { get; set; } = string.Empty;
    }
    #endregion VG
}
