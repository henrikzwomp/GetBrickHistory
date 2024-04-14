using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GetBrickHistory
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var folder_with_rebrickable_db_files = @"";

            Console.WriteLine("Reading Categories...");
            var categories = RebrickableCatalogParser.ConvertPartCategoriesLinesToList(File.ReadAllLines(folder_with_rebrickable_db_files + @"\part_categories.csv"));
            Console.WriteLine("Reading Parts...");
            var parts = RebrickableCatalogParser.ConvertPartsLinesToList(File.ReadAllLines(folder_with_rebrickable_db_files + @"\parts.csv"));
            Console.WriteLine("Reading Inventory Parts...");
            var inventory_parts = RebrickableCatalogParser.ConvertInventoryPartsLinesToList(File.ReadAllLines(folder_with_rebrickable_db_files + @"\inventory_parts.csv"));
            Console.WriteLine("Reading Inventories...");
            var inventories = RebrickableCatalogParser.ConvertInventoryLinesToList(File.ReadAllLines(folder_with_rebrickable_db_files + @"\inventories.csv"));
            Console.WriteLine("Reading Sets...");
            var sets = RebrickableCatalogParser.ConvertSetLinesToSortedList(File.ReadAllLines(folder_with_rebrickable_db_files + @"\sets.csv"));
            Console.WriteLine("Reading Elements...");
            var elements = RebrickableCatalogParser.ConvertElementLinesToList(File.ReadAllLines(folder_with_rebrickable_db_files + @"\elements.csv"));
            Console.WriteLine("Reading Colors...");
            var colors = RebrickableCatalogParser.ConvertColorLinesToList(File.ReadAllLines(folder_with_rebrickable_db_files + @"\colors.csv"));
            Console.WriteLine("Reading Inventory Minifigs...");
            var inventory_minifigs = RebrickableCatalogParser.ConvertInventoryMinifigsLinesToList(File.ReadAllLines(folder_with_rebrickable_db_files + @"\inventory_minifigs.csv"));

            var category = "Minifig Headwear";
            var part_include_filter = new List<string>() { "hair" };
            var part_exlude_filter = new List<string>() { "head top", "hat", "lady liberty", "bald top", "helmet", "hood", "homemaker"
            };

            var output = GetCategoryStats(category, part_include_filter, part_exlude_filter, categories, parts, inventory_parts, inventories, sets, elements, colors, inventory_minifigs);

            Console.WriteLine(output);
            File.WriteAllText("result.txt", output);

            Console.Beep();
            Console.ReadLine();
        }

        private static string GetCategoryStats(
            string category, List<string> part_include_filter, List<string> part_exlude_filter,
            List<RebrickablePartCategory> categories, List<RebrickablePart> parts, List<RebrickableInventoryPart> inventory_parts, List<RebrickableInventory> inventories,
            SortedList<string, RebrickableSet> sets, List<RebrickableElement> elements, List<RebrickableColor> colors, List<InventoryMinifig> inventory_minifigs)
        {
            var cat_id = categories.First(x => x.Name == category).Id;

            var my_list = new List<MyElementItem>();

            var skip_loop = false;

            var filtered_parts = parts.Where(x => x.PartCatId == cat_id);

            var inv_part_count = 0;
            var inv_part_10p = inventory_parts.Count / 10;
            var int_part_txt = 10;

            Console.WriteLine("Loop through Inventory parts list");
            foreach (var inv_part in inventory_parts)
            {
                if (my_list.Any(x => x.PartNum == inv_part.PartNum && x.ColorId == inv_part.ColorId))
                {
                    continue;
                }

                var part = filtered_parts.FirstOrDefault(x => x.PartNum == inv_part.PartNum);

                if (part == null)
                    continue;

                skip_loop = false;

                foreach (var filter in part_include_filter)
                    if (!part.Name.ToLower().Contains(filter))
                        skip_loop = true;

                foreach (var filter in part_exlude_filter)
                    if (part.Name.ToLower().Contains(filter))
                        skip_loop = true;

                if (skip_loop)
                    continue;

                Console.Write("."); // New part & color found

                my_list.Add(new MyElementItem()
                {
                    ElementId = -1,
                    PartNum = part.PartNum,
                    ColorId = inv_part.ColorId,
                    DesignId = -1,
                    PartName = part.PartNum,
                });

                inv_part_count++;
                if (inv_part_count >= inv_part_10p)
                {
                    Console.Write(int_part_txt + "%");
                    int_part_txt += 10;
                    inv_part_10p += inventory_parts.Count / 10;
                }
            }
            Console.Write(Environment.NewLine);

            Console.WriteLine("Fill in sets and element ids");
            foreach (var item in my_list)
            {
                Console.Write(".");

                var element = elements.FirstOrDefault(x => x.PartNum == item.PartNum && x.ColorId == item.ColorId);

                if (element != null)
                {
                    item.ElementId = element.ElementId;
                    item.DesignId = element.DesignId;
                }

                item.Sets = new List<RebrickableSet>();

                foreach (var inventory_part in inventory_parts.Where(x => x.PartNum == item.PartNum && x.ColorId == item.ColorId))
                {
                    var set_mf_ids = inventories.Where(x => x.Id == inventory_part.InventoryId).First().SetNum;

                    if (sets.ContainsKey(set_mf_ids)) // Is in set directly?
                    {
                        item.Sets.Add(sets[set_mf_ids]);
                    }
                    else if (inventory_minifigs.Any(x => x.FigNum == set_mf_ids)) // Is in set via minifig
                    {
                        foreach (var inventory_id in inventory_minifigs.Where(x => x.FigNum == set_mf_ids).Select(y => y.InventoryId))
                        {
                            var set_id = inventories.Where(x => x.Id == inventory_id).First().SetNum;

                            if (!item.Sets.Any(x => x.SetNum == set_id)) // Check if already exists
                                item.Sets.Add(sets[set_id]);
                        }
                    }

                    item.ImgUrl = inventory_part.ImgUrl;
                }

                if (item.Sets.Count > 0)
                {
                    item.IntroYear = item.Sets.OrderBy(x => x.Year).First().Year;
                }
            }
            Console.Write(Environment.NewLine);

            Console.WriteLine("Done!");

            // Print out by color
            var output = new StringBuilder();
            var set_count = 0;

            foreach (var color in colors.OrderBy(x => x.Name))
            {
                if (color.Id == 191)
                {
                    Console.Beep();
                    Console.Write("Blipp");
                }

                if (!my_list.Any(x => x.ColorId == color.Id && x.Sets.Any()))
                    continue;

                var parts_in_current_color = my_list.Where(x => x.ColorId == color.Id && x.Sets.Any()).ToList(); // current color and is in a set

                set_count = parts_in_current_color.Select(x => x.Sets.Count).Sum();

                var intro_year = parts_in_current_color.OrderBy(y => y.IntroYear).First().IntroYear;

                var first_in_color = parts_in_current_color.Where(x => x.IntroYear == intro_year).OrderBy(y => y.ElementId);

                output.AppendLine(color.Name + "\t" +
                    intro_year + "\t" +
                    parts_in_current_color.Count + "\t" +
                    set_count + "\t" +
                    String.Join(", ", first_in_color.Select(x => x.ElementId)) + "\t" +
                    String.Join(", ", first_in_color.Select(x => x.DesignId)) + "\t" +
                    String.Join(", ", first_in_color.Select(x => x.PartNum)));
            }

            return output.ToString();
        }

        class MyElementItem
        {
            public int ElementId { get; set; }
            public string PartNum { get; set; }
            public int ColorId { get; set; }
            public string ColorName { get; set; } // ToDo
            public int? DesignId { get; set; }
            public string PartName { get; set; }
            public List<RebrickableSet> Sets { get; set; }
            public int IntroYear { get; set; }
            public string ImgUrl { get; set; }
        }
    }
}
