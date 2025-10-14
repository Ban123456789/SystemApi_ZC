-- =============================================
-- 序列号表 - 用于管理各表的自动编号
-- =============================================

CREATE TABLE sequenceNumbers (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    tableName NVARCHAR(100) NOT NULL,
    groupKey NVARCHAR(200) NOT NULL,
    currentNumber INT NOT NULL DEFAULT 0,
    numberFormat NVARCHAR(50) NULL DEFAULT '0000',
    description NVARCHAR(500) NULL,
    createdOn DATETIME2 NOT NULL DEFAULT GETDATE(),
    modifiedOn DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_Sequence UNIQUE (tableName, groupKey)
);

-- 创建索引以提高查询性能
CREATE INDEX IX_sequenceNumbers_TableGroup ON sequenceNumbers(tableName, groupKey);

GO

PRINT '序列号表创建成功！';

