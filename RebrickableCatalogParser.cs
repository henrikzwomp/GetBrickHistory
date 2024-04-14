using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetBrickHistory
{
    public class RebrickableCatalogParser
    {
        public static List<RebrickablePartCategory> ConvertPartCategoriesLinesToList(string[] file_lines)
        {
            return GetList<RebrickablePartCategory>(file_lines);
        }

        public static List<RebrickablePart> ConvertPartsLinesToList(string[] file_lines)
        {
            return GetList<RebrickablePart>(file_lines);
        }
        public static List<RebrickableInventoryPart> ConvertInventoryPartsLinesToList(string[] file_lines)
        {
            return GetList<RebrickableInventoryPart>(file_lines);
        }

        public static List<RebrickableInventory> ConvertInventoryLinesToList(string[] file_lines)
        {
            return GetList<RebrickableInventory>(file_lines);
        }

        public static SortedList<string, RebrickableSet> ConvertSetLinesToSortedList(string[] file_lines)
        {
            return GetSortedList<RebrickableSet>(file_lines, "SetNum");
        }

        public static List<RebrickableElement> ConvertElementLinesToList(string[] file_lines)
        {
            return GetList<RebrickableElement>(file_lines);
        }

        public static List<RebrickableColor> ConvertSetLinesToList(string[] file_lines)
        {
            return GetList<RebrickableColor>(file_lines);
        }

        public static List<RebrickableColor> ConvertColorLinesToList(string[] file_lines)
        {
            return GetList<RebrickableColor>(file_lines);
        }

        // ConvertInventoryMinifigsLinesToList
        public static List<InventoryMinifig> ConvertInventoryMinifigsLinesToList(string[] file_lines)
        {
            return GetList<InventoryMinifig>(file_lines);
        }

        private static List<LineType> GetList<LineType>(string[] file_lines) where LineType : new()
        {
            if (!VerifyHeaderLineFor<LineType>(file_lines[0]))
                throw new Exception("First line with headers different than expected.");

            var baselist = ConvertLinesTo<LineType>(
                ProcessLines<LineType>(file_lines.Skip(1)));

            return baselist;
        }

        private static SortedList<string, LineType> GetSortedList<LineType>(string[] file_lines, string key_column) where LineType : new()
        {
            var baselist = GetList<LineType>(file_lines);

            var result = new SortedList<string, LineType>();

            var key_property = typeof(LineType).GetProperties().First(x => x.Name == key_column);

            foreach (var line in baselist)
                result.Add(key_property.GetValue(line).ToString(), line);

            return result;
        }

        private static List<line_class> ConvertLinesTo<line_class>(IEnumerable<string[]> parsed_lines) where line_class : new()
        {
            var class_properties = typeof(line_class).GetProperties();

            var result = new List<line_class>();

            foreach (var line in parsed_lines)
            {
                var new_item = new line_class();

                for (int i = 0; i < class_properties.Length; i++)
                {
                    if (class_properties[i].PropertyType == typeof(string))
                    {
                        class_properties[i].SetValue(new_item, line[i]);
                    }
                    else if (class_properties[i].PropertyType == typeof(int))
                    {
                        class_properties[i].SetValue(new_item, int.Parse(line[i]));
                    }
                    else if (class_properties[i].PropertyType == typeof(int?))
                    {
                        if (line[i] == "")
                            class_properties[i].SetValue(new_item, null);
                        else
                            class_properties[i].SetValue(new_item, int.Parse(line[i]));
                    }
                    else if (class_properties[i].PropertyType == typeof(bool))
                    {
                        var bool_value = false;

                        if (line[i].ToLower() == "t")
                            bool_value = true;

                        class_properties[i].SetValue(new_item, bool_value);
                    }
                    else
                    {
                        throw new Exception("PropertyType not supported");
                    }
                }

                result.Add(new_item);
            }

            return result;
        }

        private static List<string[]> ProcessLines<line_class>(IEnumerable<string> lines)
        {
            var separator = ',';

            var result = new List<string[]>();

            foreach (string line in lines)
            {
                if (line.Contains("\""))
                {
                    var quote_on = false;
                    var part = "";
                    var parts = new List<string>();

                    foreach (char linechar in line)
                    {
                        if (linechar == '"')
                        {
                            quote_on = !quote_on;
                        }
                        else if (linechar == separator)
                        {
                            if (quote_on == false)
                            {
                                parts.Add(part);
                                part = "";
                            }
                            else
                            {
                                part += linechar;
                            }
                        }
                        else
                        {
                            part += linechar;
                        }

                    }
                    parts.Add(part);

                    result.Add(parts.ToArray());
                }
                else
                {
                    result.Add(line.Split(separator));
                }
            }
            return result;
        }

        private static bool VerifyHeaderLineFor<line_class>(string header_line)
        {
            var separator = ',';

            header_line = header_line.Replace(" ", "");
            header_line = header_line.Replace("_", "");

            var input_headers = header_line.Split(separator);

            var class_properties = typeof(line_class).GetProperties();

            if (input_headers.Length != class_properties.Length)
                return false;

            for (int i = 0; i < class_properties.Length; i++)
            {
                if (class_properties[i].Name.ToLower() != input_headers[i].ToLower())
                    return false;
            }

            return true;
        }
    }

    public class RebrickablePartCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class RebrickablePart
    {
        public string PartNum { get; set; }
        public string Name { get; set; }
        public int PartCatId { get; set; }
        public string PartMaterial { get; set; }
    }

    public class RebrickableInventoryPart
    {
        public int InventoryId { get; set; }
        public string PartNum { get; set; }
        public int ColorId { get; set; }
        public int Quantity { get; set; }
        public bool IsSpare { get; set; }
        public string ImgUrl { get; set; }
    }

    public class RebrickableInventory
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public string SetNum { get; set; }
    }

    public class RebrickableSet
    {
        public string SetNum { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }
        public int ThemeId { get; set; }
        public int NumParts { get; set; }
        public string ImgUrl { get; set; }
    }

    public class RebrickableElement
    {
        public int ElementId { get; set; }
        public string PartNum { get; set; }
        public int ColorId { get; set; }
        public int? DesignId { get; set; }
    }

    public class RebrickableColor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RGB { get; set; }
        public bool IsTrans { get; set; }
    }

    public class InventoryMinifig
    {
        public int InventoryId { get; set; }
        public string FigNum { get; set; }
        public int Quantity { get; set; }
    }
}
