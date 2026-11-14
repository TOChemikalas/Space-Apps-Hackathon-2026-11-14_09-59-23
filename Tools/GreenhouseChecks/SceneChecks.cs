using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

static class SceneChecks
{
    static int passed;
    static void Assert(bool condition, string label)
    {
        if (!condition) throw new Exception("Scene: " + label);
        passed++;
    }
    static string Field(string block, string field) => Regex.Match(block, @"(?m)^  " + field + @": (.*)$").Groups[1].Value.Trim();
    static long Reference(string field) => long.Parse(Regex.Match(field, @"fileID: (-?\d+)").Groups[1].Value);
    static (double x, double y) Vector(string field)
    {
        var match = Regex.Match(field, @"x: ([-.\d]+), y: ([-.\d]+)");
        return (double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
    }
    record Box(double X, double Y, double W, double H, bool Trigger, long Id);

    public static void Run()
    {
        string root = Directory.GetCurrentDirectory();
        while (!Directory.Exists(Path.Combine(root, "Assets"))) root = Directory.GetParent(root)?.FullName ?? throw new Exception("Run inside repository");
        string scene = File.ReadAllText(Path.Combine(root, "Assets/Scenes/SampleScene.unity"));
        Assert(!scene.Contains("<<<<<<<") && !scene.Contains(">>>>>>>"), "No unresolved scene conflicts");
        var docs = Regex.Matches(scene, @"(?ms)^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\z)");
        var blocks = new Dictionary<long, string>();
        var types = new Dictionary<long, int>();
        foreach (Match doc in docs)
        {
            long id = long.Parse(doc.Groups[2].Value);
            Assert(blocks.TryAdd(id, doc.Groups[3].Value), "Unique object ID " + id);
            types[id] = int.Parse(doc.Groups[1].Value);
        }
        foreach (Match reference in Regex.Matches(scene, @"\{fileID: (-?\d+)\}"))
        {
            long id = long.Parse(reference.Groups[1].Value);
            Assert(id == 0 || blocks.ContainsKey(id), "Local reference resolves: " + id);
        }
        var metaFiles = Directory.EnumerateFiles(Path.Combine(root, "Assets"), "*.meta", SearchOption.AllDirectories).ToArray();
        var guidToPath = new Dictionary<string, string>();
        foreach (var path in metaFiles)
        {
            string text = File.ReadAllText(path);
            var guid = Regex.Match(text, @"(?m)^guid: ([a-f0-9]+)").Groups[1].Value;
            if (guid.Length > 0) guidToPath[guid] = path[..^5];
        }
        string ScriptGuid(string name) => Regex.Match(File.ReadAllText(Path.Combine(root, $"Assets/Scripts/Greenhouse/{name}.cs.meta")), @"guid: ([a-f0-9]+)").Groups[1].Value;
        string bedGuid = ScriptGuid("GrowingBed");
        var beds = blocks.Where(pair => types[pair.Key] == 114 && pair.Value.Contains("guid: " + bedGuid)).ToArray();
        Assert(beds.Length == 4, "Four configured beds");
        string menuGuid = ScriptGuid("GrowingBedMenu");
        var menus = blocks.Where(pair => types[pair.Key] == 114 && pair.Value.Contains("guid: " + menuGuid)).ToArray();
        Assert(menus.Length == beds.Length, "One separate menu instance per bed");
        foreach (var bed in beds)
        {
            long owner = Reference(Field(bed.Value, "m_GameObject"));
            Assert(menus.Count(menu => Reference(Field(menu.Value, "m_GameObject")) == owner) == 1,
                "Bed owns exactly one menu: " + owner);
        }
        string clockGuid = Regex.Match(File.ReadAllText(Path.Combine(root, "Assets/Scripts/MarsClock.cs.meta")), @"guid: ([a-f0-9]+)").Groups[1].Value;
        Assert(blocks.Count(pair => types[pair.Key] == 114 && pair.Value.Contains("guid: " + clockGuid)) == 1, "One authoritative clock");
        foreach (var name in new[] { "lettuce", "radish", "potato", "wheat" })
        {
            string asset = File.ReadAllText(Path.Combine(root, $"Assets/Data/Crops/{name}.asset"));
            var sprites = Regex.Matches(asset, @"fileID: 21300000, guid: ([a-f0-9]+)");
            Assert(sprites.Count == 6, name + " has six sprites");
            foreach (Match sprite in sprites)
                Assert(guidToPath.TryGetValue(sprite.Groups[1].Value, out var path) && File.Exists(path), name + " sprite exists");
        }
        foreach (var block in blocks.Where(pair => pair.Key >= 2100000000 && pair.Key < 2200000000))
            foreach (Match guid in Regex.Matches(block.Value, @"guid: ([a-f0-9]{32})"))
                Assert(guid.Groups[1].Value.StartsWith("00000000") || guidToPath.ContainsKey(guid.Groups[1].Value), "Greenhouse asset resolves");

        var transforms = blocks.Where(pair => types[pair.Key] == 4).ToDictionary(pair => Reference(Field(pair.Value, "m_GameObject")), pair => pair.Key);
        (double x, double y, double sx, double sy) Pose(long transform)
        {
            var block = blocks[transform];
            var local = Vector(Field(block, "m_LocalPosition"));
            var scale = Vector(Field(block, "m_LocalScale"));
            long parent = Reference(Field(block, "m_Father"));
            if (parent == 0) return (local.x, local.y, scale.x, scale.y);
            var ancestor = Pose(parent);
            return (ancestor.x + local.x * ancestor.sx, ancestor.y + local.y * ancestor.sy, ancestor.sx * scale.x, ancestor.sy * scale.y);
        }
        var boxes = new List<Box>();
        foreach (var pair in blocks.Where(pair => types[pair.Key] == 61 && pair.Key >= 2100000000))
        {
            var pose = Pose(transforms[Reference(Field(pair.Value, "m_GameObject"))]);
            var size = Vector(Field(pair.Value, "m_Size"));
            boxes.Add(new Box(pose.x, pose.y, size.x * pose.sx, size.y * pose.sy, Field(pair.Value, "m_IsTrigger") == "1", pair.Key));
        }
        bool Blocked(double x, double y) => boxes.Any(box => !box.Trigger && Math.Abs(x - box.X) < box.W / 2 + .5 - 1e-6 && Math.Abs(y - box.Y) < box.H / 2 + .5 - 1e-6);
        var spawn = Pose(2100002001);
        var outsideSpawn = Pose(2100002011);
        Assert(!Blocked(spawn.x, spawn.y), "Interior arrival clear for existing 1x1 player collider");
        Assert(!Blocked(outsideSpawn.x, outsideSpawn.y), "Exterior arrival clear");
        foreach (var spawnPoint in new[] { spawn, outsideSpawn })
            Assert(!boxes.Any(box => box.Trigger && (box.Id == 2100002022 || box.Id == 2100002032)
                && Math.Abs(spawnPoint.x - box.X) < box.W / 2 + .5 && Math.Abs(spawnPoint.y - box.Y) < box.H / 2 + .5), "Arrival outside both doorway triggers");
        var reached = new HashSet<(int x, int y)>();
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((0, 0)); reached.Add((0, 0));
        while (queue.TryDequeue(out var cell))
            foreach (var next in new[] { (cell.x + 1, cell.y), (cell.x - 1, cell.y), (cell.x, cell.y + 1), (cell.x, cell.y - 1) })
            {
                double x = spawn.x + next.Item1 * .1, y = spawn.y + next.Item2 * .1;
                if (x < 88 || x > 112 || y < -5.8 || y > 4 || Blocked(x, y) || !reached.Add(next)) continue;
                queue.Enqueue(next);
            }
        foreach (var bed in beds)
        {
            long solid = Reference(Field(bed.Value, "solidBoundary"));
            var trigger = boxes.Single(box => box.Id == solid + 1);
            Assert(reached.Any(cell => Math.Abs(spawn.x + cell.x * .1 - trigger.X) < trigger.W / 2 + .49
                && Math.Abs(spawn.y + cell.y * .1 - trigger.Y) < trigger.H / 2 + .49), "Walkable route to bed " + bed.Key);
        }
        var exit = boxes.Single(box => box.Id == 2100002032);
        Assert(reached.Any(cell => Math.Abs(spawn.x + cell.x * .1 - exit.X) < exit.W / 2
            && Math.Abs(spawn.y + cell.y * .1 - exit.Y) < exit.H / 2), "Walkable route to exit");
        Console.WriteLine($"PASS: {passed} serialized scene/asset assertions, including geometric walkway reachability (not a Unity physics test).");
    }
}
