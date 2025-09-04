using System;
using System.Collections.Generic;

namespace U0UGames.FeiShu.Editor
{
    /// <summary>
    /// 飞书API相关的数据模型
    /// </summary>
    
    [System.Serializable]
    public class ExportTask
    {
        public string file_extension;
        public string token;
        public string type;
        public string sub_id;
    }

    [System.Serializable]
    public class CreateExportTaskResponse
    {
        public int code;
        public string msg;
        public string log_id;
        public ExportTaskData data;
        public bool success => code == 0;
    }

    [System.Serializable]
    public class ExportTaskData
    {
        public string ticket;
    }

    // 查询导出任务结果的数据模型
    [System.Serializable]
    public class GetExportTaskResponse
    {
        public int code;
        public string msg;
        public ExportTaskResultData data;
        public bool success => code == 0;
    }
    [System.Serializable]
    public class ExportTaskResultData
    {
        public ExportTaskResultDataResult result;
    }

    [System.Serializable]
    public class ExportTaskResultDataResult
    {
        public string file_extension;
        public string type;
        public string file_name;
        public string file_token;
        public int file_size;
        public string job_error_msg;
        public int job_status;
    }

    // 下载导出文件的数据模型
    [System.Serializable]
    public class DownloadExportRequest
    {
        public string fileToken;
    }

    [System.Serializable]
    public class DownloadFileData
    {
        public string file_name;
        public string file_token;
        public string file_size;
        public string file_type;
        public byte[] file_content; // 添加文件内容字段，用于直接下载
    }

    [System.Serializable]
    public class DownloadExportResponse
    {
        public int code;
        public string msg;
        public DownloadFileData data;
        public bool success => code == 0;
    }

    // 查询表格数据的数据模型
    [System.Serializable]
    public class QuerySpreadsheetSheetResponse
    {
        public int code;
        public string msg;
        public SpreadsheetData data;
        public bool success => code == 0;
    }

    [System.Serializable]
    public class SpreadsheetData
    {
        public List<SheetInfo> sheets;
    }

    [System.Serializable]
    public class SheetInfo
    {
        public GridProperties grid_properties;
        public bool hidden;
        public int index;
        public string resource_type;
        public string sheet_id;
        public string title;
    }

    [System.Serializable]
    public class GridProperties
    {
        public int column_count;
        public int frozen_column_count;
        public int frozen_row_count;
        public int row_count;
    }

    // OAuth2授权相关的数据模型 - 适配飞书实际响应格式
    [System.Serializable]
    public class OAuthTokenResponse
    {
        public int code;
        public string access_token;
        public int expires_in;
        public string refresh_token;
        public int refresh_token_expires_in;
        public string token_type;
        public string scope;
        public bool success => code == 0;
    }

    [System.Serializable]
    public class UserInfoResponse
    {
        public int code;
        public string msg;
        public UserData data;
        public bool success => code == 0;
    }

    [System.Serializable]
    public class UserData
    {
        public string name;
        public string avatar_url;
        public string email;
        public string user_id;
    }

    // 分片上传相关的数据模型
    [System.Serializable]
    public class UploadPrepareRequest
    {
        public string file_name;
        public string parent_node;
        public string parent_type;
        public int size;
    }

    [System.Serializable]
    public class UploadPrepareResponse
    {
        public int code;
        public string msg;
        public UploadPrepareData data;
        public bool success => code == 0;
    }

    [System.Serializable]
    public class UploadPrepareData
    {
        public string upload_id;
        public int block_size;
        public int block_num;
    }


    [System.Serializable]
    public class UploadPartResponse
    {
        public int code;
        public string msg;
        public bool success => code == 0;
    }

    [System.Serializable]
    public class UploadFinishRequest
    {
        public string upload_id;
        public int block_num;
    }

    [System.Serializable]
    public class UploadFinishResponse
    {
        public int code;
        public string msg;
        public UploadFinishData data;
        public bool success => code == 0;
    }

    [System.Serializable]
    public class UploadFinishData
    {
        public string file_token;
    }
}
