using ClassDutyHelper.Models;
using OfficeOpenXml;
using System.IO;

namespace ClassDutyHelper.Services
{
    public class ExcelService
    {
        public ExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        #region Student Import/Export
        public List<Student> ImportStudents(string filePath, int nameCol, int? studentIdCol, int? groupCol, out string error)
        {
            error = string.Empty;
            var students = new List<Student>();

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    var name = worksheet.Cells[row, nameCol]?.Text?.Trim();
                    if (string.IsNullOrEmpty(name))
                        continue;

                    var student = new Student
                    {
                        Name = name,
                        StudentId = studentIdCol.HasValue ? worksheet.Cells[row, studentIdCol.Value]?.Text?.Trim() : null,
                        Group = groupCol.HasValue ? worksheet.Cells[row, groupCol.Value]?.Text?.Trim() : null,
                        IsEnabled = true
                    };

                    students.Add(student);
                }
            }
            catch (Exception ex)
            {
                error = $"导入失败：{ex.Message}";
            }

            return students;
        }

        public string ExportStudents(List<Student> students, string? savePath = null)
        {
            savePath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"学生名单_{DateTime.Now:yyyyMMdd}.xlsx");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("学生名单");

            worksheet.Cells[1, 1].Value = "姓名";
            worksheet.Cells[1, 2].Value = "学号";
            worksheet.Cells[1, 3].Value = "值日组";
            worksheet.Cells[1, 4].Value = "是否启用";

            for (int i = 0; i < students.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = students[i].Name;
                worksheet.Cells[i + 2, 2].Value = students[i].StudentId;
                worksheet.Cells[i + 2, 3].Value = students[i].Group;
                worksheet.Cells[i + 2, 4].Value = students[i].IsEnabled ? "是" : "否";
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(savePath));

            return savePath;
        }
        #endregion

        #region DutyProject Import/Export
        public List<DutyProject> ImportDutyProjects(string filePath, int nameCol, int? countCol, out string error)
        {
            error = string.Empty;
            var projects = new List<DutyProject>();

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    var name = worksheet.Cells[row, nameCol]?.Text?.Trim();
                    if (string.IsNullOrEmpty(name))
                        continue;

                    var project = new DutyProject
                    {
                        Name = name,
                        DefaultPersonCount = countCol.HasValue && int.TryParse(worksheet.Cells[row, countCol.Value]?.Text, out var count) ? count : 1,
                        IsEnabled = true
                    };

                    projects.Add(project);
                }
            }
            catch (Exception ex)
            {
                error = $"导入失败：{ex.Message}";
            }

            return projects;
        }

        public string ExportDutyProjects(List<DutyProject> projects, string? savePath = null)
        {
            savePath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"值日项目_{DateTime.Now:yyyyMMdd}.xlsx");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("值日项目");

            worksheet.Cells[1, 1].Value = "项目名称";
            worksheet.Cells[1, 2].Value = "默认人数";
            worksheet.Cells[1, 3].Value = "是否启用";

            for (int i = 0; i < projects.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = projects[i].Name;
                worksheet.Cells[i + 2, 2].Value = projects[i].DefaultPersonCount;
                worksheet.Cells[i + 2, 3].Value = projects[i].IsEnabled ? "是" : "否";
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(savePath));

            return savePath;
        }
        #endregion

        #region DutyRecord Import/Export
        public List<DutyRecordImport> ImportDutyRecords(string filePath, out string error)
        {
            error = string.Empty;
            var records = new List<DutyRecordImport>();

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    var dateStr = worksheet.Cells[row, 1]?.Text?.Trim();
                    var projectName = worksheet.Cells[row, 2]?.Text?.Trim();
                    var studentNames = worksheet.Cells[row, 3]?.Text?.Trim();
                    var countStr = worksheet.Cells[row, 4]?.Text?.Trim();
                    var remark = worksheet.Cells[row, 5]?.Text?.Trim();

                    if (string.IsNullOrEmpty(dateStr) || string.IsNullOrEmpty(projectName))
                        continue;

                    if (!DateTime.TryParse(dateStr, out var date))
                    {
                        error = $"第{row}行日期格式错误：{dateStr}";
                        continue;
                    }

                    var record = new DutyRecordImport
                    {
                        Date = date,
                        ProjectName = projectName,
                        StudentNames = studentNames?.Split(new[] { '、', ',', '，', '/' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(),
                        PersonCount = int.TryParse(countStr, out var count) ? count : 0,
                        Remark = remark
                    };

                    records.Add(record);
                }
            }
            catch (Exception ex)
            {
                error = $"导入失败：{ex.Message}";
            }

            return records;
        }

        public string ExportDutyRecords(List<DutyRecord> records, string className, string? savePath = null)
        {
            savePath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"{className}班值日表_{DateTime.Now:yyyyMMdd}.xlsx");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("值日表");

            worksheet.Cells[1, 1].Value = "日期";
            worksheet.Cells[1, 2].Value = "值日项目";
            worksheet.Cells[1, 3].Value = "参与人员";
            worksheet.Cells[1, 4].Value = "人数";
            worksheet.Cells[1, 5].Value = "备注";

            for (int i = 0; i < records.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = records[i].DutyDate.ToString("yyyy-MM-dd");
                worksheet.Cells[i + 2, 2].Value = records[i].DutyProject?.Name;
                worksheet.Cells[i + 2, 3].Value = records[i].Student?.Name;
                worksheet.Cells[i + 2, 4].Value = 1;
                worksheet.Cells[i + 2, 5].Value = records[i].Remark;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(savePath));

            return savePath;
        }
        #endregion
    }

    public class DutyRecordImport
    {
        public DateTime Date { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string[] StudentNames { get; set; } = Array.Empty<string>();
        public int PersonCount { get; set; }
        public string? Remark { get; set; }
    }
}
