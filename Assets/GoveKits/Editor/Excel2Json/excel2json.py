import pandas as pd
import json
import os
import argparse
from pathlib import Path
import logging
from datetime import datetime

def setup_logging():
    """
    设置日志记录器
    """
    # 获取脚本所在目录
    script_dir = Path(__file__).parent
    log_dir = script_dir.parent / "Log"
    
    # 创建日志目录
    log_dir.mkdir(exist_ok=True)
    
    # 生成日志文件名
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    log_file = log_dir / f"excel2json_{timestamp}.log"
    
    # 配置日志格式
    log_format = "%(asctime)s - %(levelname)s - %(message)s"
    
    # 设置日志记录器
    logging.basicConfig(
        level=logging.INFO,
        format=log_format,
        handlers=[
            logging.FileHandler(log_file, encoding='utf-8'),
            logging.StreamHandler()  # 同时输出到控制台
        ]
    )
    
    logger = logging.getLogger(__name__)
    logger.info(f"日志文件已创建: {log_file}")
    return logger

def safe_print(message):
    """
    安全的打印函数，处理编码问题
    """
    try:
        print(message)
    except UnicodeEncodeError:
        # 移除表情符号和特殊字符
        safe_message = message.encode('ascii', 'ignore').decode('ascii')
        print(safe_message)

def excel_to_json(excel_path, key_column="key", output_json_path=None, logger=None):
    """
    将Excel表转换为JSON格式
    
    参数:
        excel_path (str): Excel文件路径
        key_column (str): 键名的列（如"Key"）
        output_json_path (str): 输出的JSON文件路径（可选）
        logger: 日志记录器
    
    返回:
        dict: 转换后的字典
    """
    if logger is None:
        logger = logging.getLogger(__name__)
        
    try:
        logger.info(f"开始转换文件: {excel_path}")
        
        # 读取Excel文件
        df = pd.read_excel(excel_path)
        logger.info(f"成功读取Excel文件，共 {len(df)} 行数据")
        
        # 检查Key列是否存在
        if key_column not in df.columns:
            error_msg = f"列 '{key_column}' 不存在，可用列: {list(df.columns)}"
            logger.error(error_msg)
            raise ValueError(error_msg)
        
        # 获取所有数据列（排除Key列）
        data_columns = [col for col in df.columns if col != key_column]
        
        if not data_columns:
            error_msg = "没有找到数据列，请确认Excel结构！"
            logger.error(error_msg)
            raise ValueError(error_msg)
        
        logger.info(f"发现数据列: {data_columns}")
        
        # 构建字典 {column: {key: value}}
        result = {}
        for column in data_columns:
            # 过滤掉空值的行
            valid_data = df.dropna(subset=[key_column, column])
            result[column] = dict(zip(valid_data[key_column], valid_data[column]))
            logger.info(f"列 '{column}' 转换完成，共 {len(result[column])} 个键值对")
        
        # 输出到文件或返回字典
        if output_json_path:
            # 确保输出目录存在
            output_dir = os.path.dirname(output_json_path)
            if output_dir:
                os.makedirs(output_dir, exist_ok=True)
            
            with open(output_json_path, "w", encoding="utf-8") as f:
                json.dump(result, f, ensure_ascii=False, indent=2)
            
            success_msg = f"[SUCCESS] JSON已保存至: {output_json_path}"
            safe_print(success_msg)
            logger.info(success_msg)
        
        logger.info(f"文件转换成功: {excel_path}")
        return result
        
    except Exception as e:
        error_msg = f"[ERROR] 转换失败 {excel_path}: {str(e)}"
        safe_print(error_msg)
        logger.error(error_msg)
        logger.exception("详细错误信息:")
        return None

def process_directory(input_dir, output_dir, key_column="key", logger=None):
    """
    递归处理目录下的所有Excel文件
    
    参数:
        input_dir (str): 输入目录路径
        output_dir (str): 输出目录路径
        key_column (str): 键名的列
        logger: 日志记录器
    """
    if logger is None:
        logger = logging.getLogger(__name__)
        
    input_path = Path(input_dir)
    output_path = Path(output_dir)
    
    logger.info(f"开始处理目录: {input_dir}")
    logger.info(f"输出目录: {output_dir}")
    logger.info(f"键名列: {key_column}")
    
    if not input_path.exists():
        error_msg = f"[ERROR] 输入目录不存在: {input_dir}"
        safe_print(error_msg)
        logger.error(error_msg)
        return
    
    # 支持的Excel文件扩展名
    excel_extensions = ['.xlsx', '.xls', '.xlsm']
    logger.info(f"支持的文件格式: {excel_extensions}")
    
    # 递归查找所有Excel文件
    excel_files = []
    for ext in excel_extensions:
        found_files = list(input_path.rglob(f"*{ext}"))
        excel_files.extend(found_files)
        logger.info(f"找到 {len(found_files)} 个 {ext} 文件")
    
    if not excel_files:
        warning_msg = f"[WARNING] 在目录 {input_dir} 中没有找到Excel文件"
        safe_print(warning_msg)
        logger.warning(warning_msg)
        return
    
    info_msg = f"[INFO] 总共找到 {len(excel_files)} 个Excel文件"
    safe_print(info_msg)
    logger.info(info_msg)
    
    success_count = 0
    failed_count = 0
    
    for i, excel_file in enumerate(excel_files, 1):
        # 计算相对路径
        relative_path = excel_file.relative_to(input_path)
        
        # 构建输出JSON文件路径
        json_file = output_path / relative_path.with_suffix('.json')
        
        progress_msg = f"[PROGRESS] 正在转换 ({i}/{len(excel_files)}): {relative_path}"
        safe_print(progress_msg)
        logger.info(progress_msg)
        
        # 转换Excel到JSON
        result = excel_to_json(
            str(excel_file), 
            key_column=key_column, 
            output_json_path=str(json_file),
            logger=logger
        )
        
        if result is not None:
            success_count += 1
            logger.info(f"文件转换成功: {relative_path}")
        else:
            failed_count += 1
            logger.error(f"文件转换失败: {relative_path}")
    
    # 输出最终统计
    final_msg = f"\n[SUMMARY] 转换完成:\n[SUCCESS] 成功: {success_count} 个文件\n[FAILED] 失败: {failed_count} 个文件"
    safe_print(final_msg)
    logger.info(f"转换任务完成 - 成功: {success_count}, 失败: {failed_count}")
    
    if failed_count > 0:
        logger.warning(f"有 {failed_count} 个文件转换失败，请检查日志获取详细信息")

def main():
    """
    命令行入口函数
    """
    # 设置日志记录器
    logger = setup_logging()
    
    parser = argparse.ArgumentParser(
        description="Excel转JSON工具 - 递归转换目录下的所有Excel文件",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
使用示例:
  python excel2json.py -i ./excel_files -o ./json_files
  python excel2json.py -i ./excel_files -o ./json_files -k "Key"
        """
    )
    
    parser.add_argument(
        '-i', '--input', 
        required=True, 
        help='输入目录路径（包含Excel文件）'
    )
    
    parser.add_argument(
        '-o', '--output', 
        required=True, 
        help='输出目录路径（保存JSON文件）'
    )
    
    parser.add_argument(
        '-k', '--key-column', 
        default='key', 
        help='Excel中键名列的名称（默认: "key"）'
    )
    
    args = parser.parse_args()
    
    # 记录启动信息
    logger.info("=" * 60)
    logger.info("Excel2JSON 转换工具启动")
    logger.info(f"输入目录: {args.input}")
    logger.info(f"输出目录: {args.output}")
    logger.info(f"键名列: {args.key_column}")
    logger.info("=" * 60)
    
    safe_print("Excel2JSON 转换工具")
    safe_print(f"输入目录: {args.input}")
    safe_print(f"输出目录: {args.output}")
    safe_print(f"键名列: {args.key_column}")
    safe_print("-" * 50)
    
    try:
        # 开始处理
        process_directory(args.input, args.output, args.key_column, logger)
        logger.info("程序执行完成")
    except Exception as e:
        logger.error(f"程序执行出错: {str(e)}")
        logger.exception("详细错误信息:")
        raise

if __name__ == "__main__":
    main()