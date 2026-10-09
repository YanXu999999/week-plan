namespace WeekPlan;

public readonly record struct FoodPortion(string Label, double Amount)
{
    public string Describe(string unit) =>
        $"{Label}约 {NutritionMath.FormatNumber(Amount)} {NutritionMath.UnitLabel(unit)}";
}

public static class NutritionPortions
{
    // 家用估算：能数的按一根、一个的可食部，散装按一碗、一把、一勺。
    private static readonly Dictionary<string, FoodPortion> Exact = CreateExact();

    public static FoodPortion For(string name, string unit) =>
        Exact.TryGetValue(name, out var portion)
            ? portion
            : unit == NutritionUnits.Milliliter ? Liquid(name) : Solid(name);

    private static FoodPortion Liquid(string name)
    {
        if (name.Contains("酱油", StringComparison.Ordinal)
            || name.Contains('抽')
            || name.Contains('醋')
            || name.Contains("豉油", StringComparison.Ordinal)
            || name is "料酒" or "蚝油" or "鱼露" or "虾油" or "味极鲜")
            return new("一勺", 15);
        if (name.Contains('油'))
            return new("一勺", 10);
        if (name is "白酒")
            return new("一小杯", 50);
        if (name is "啤酒")
            return new("一罐", 330);
        if (name.Contains('酒'))
            return new("一杯", 100);
        if (name is "可乐" or "雪碧")
            return new("一罐", 330);
        if (name.Contains("奶茶", StringComparison.Ordinal)
            || name.Contains("奶昔", StringComparison.Ordinal)
            || name is "拿铁" or "卡布奇诺" or "摩卡")
            return new("一杯", 350);
        return new("一杯", 250);
    }

    private static FoodPortion Solid(string name)
    {
        if (name is "白糖" or "红糖" or "冰糖" or "蜂蜜")
            return new("一勺", 10);
        if (name is "盐" or "味精" or "鸡精")
            return new("一小撮", 2);
        if (DrySpices.Contains(name))
            return new("一小撮", 2);
        if (name.EndsWith('酱') || name is "老干妈" or "郫县豆瓣")
            return new("一勺", 15);
        if (DryGrains.Contains(name))
            return new("一把", 80);
        if (DryBeans.Contains(name))
            return new("一把", 50);
        if (Seeds.Contains(name))
            return new("一勺", 10);
        if (Nuts.Contains(name))
            return new("一把", 20);
        if (IsPowder(name))
            return new("一勺", 15);

        var piece = Piece(name);
        if (piece is not null)
            return piece.Value;
        if (IsDish(name))
            return new("一盘", 200);
        if (name.EndsWith('鱼') || name.EndsWith('鳗') || name.EndsWith('鳅') || name.EndsWith('鲹') || name.EndsWith('鲀') || name.EndsWith('鳝'))
            return new("一条", 150);
        if (name.EndsWith('虾'))
            return new("一只", 15);
        if (name.Contains('蟹'))
            return new("一只", 80);
        if (name.EndsWith("贝") || name.EndsWith("蛤") || name.EndsWith("蚝") || name.EndsWith("螺") || name.EndsWith("鲍"))
            return new("一只", 20);
        if (name.EndsWith('蛋'))
            return new("一个", 50);
        if (name.EndsWith("火腿肠") || name.EndsWith("香肠"))
            return new("一根", 50);
        if (name.EndsWith("火腿") || name is "腊肉" or "培根" or "午餐肉" or "肉松" or "肉干" or "肉脯" or "叉烧")
            return new("一份", 40);

        var cut = Cut(name);
        if (cut is not null)
            return cut.Value;
        if (name.Length >= 4)
            return new("一盘", 200);
        return new("一份", 100);
    }

    private static FoodPortion? Piece(string name)
    {
        if (name.EndsWith("油条", StringComparison.Ordinal))
            return new("一根", 70);
        if (name.EndsWith("麻花", StringComparison.Ordinal))
            return new("一根", 35);
        if (name.EndsWith("面包", StringComparison.Ordinal))
            return new("一片", 40);
        if (name.EndsWith("烧饼", StringComparison.Ordinal))
            return new("一个", 60);
        if (name.EndsWith("火烧", StringComparison.Ordinal))
            return new("一个", 120);
        if (name.EndsWith("锅盔", StringComparison.Ordinal) || name.EndsWith('馕'))
            return new("一块", 100);
        if (name.EndsWith("小笼", StringComparison.Ordinal) || name.EndsWith("烧麦", StringComparison.Ordinal))
            return new("一个", 40);
        if (name.EndsWith("馒头", StringComparison.Ordinal))
            return new("一个", 100);
        if (name.EndsWith("花卷", StringComparison.Ordinal))
            return new("一个", 80);
        if (name.EndsWith("包子", StringComparison.Ordinal) || name.EndsWith('包'))
            return new("一个", 80);
        if (name.EndsWith("饺子", StringComparison.Ordinal) || name.EndsWith('饺'))
            return new("一个", 25);
        if (name.EndsWith("馄饨", StringComparison.Ordinal))
            return new("一个", 20);
        if (name.EndsWith("汤圆", StringComparison.Ordinal))
            return new("一个", 30);
        if (name.EndsWith('粽') || name.EndsWith("粽子", StringComparison.Ordinal) || name.EndsWith("肉粽", StringComparison.Ordinal))
            return new("一个", 200);
        if (name is "面片")
            return new("一碗", 200);
        if (name.EndsWith('碗'))
            return new("一碗", 300);
        if (name.EndsWith('馍'))
            return new("一个", 150);
        if (name.EndsWith("麦满分", StringComparison.Ordinal))
            return new("一个", 140);
        if (name.EndsWith('堡'))
            return new("一个", 180);
        if (name.EndsWith('卷'))
            return new("一个", 180);
        if (name.EndsWith('派'))
            return new("一个", 80);
        if (name.EndsWith('挞'))
            return new("一个", 60);
        if (name.EndsWith('糕') || name.EndsWith('粑'))
            return new("一块", 80);
        if (name.EndsWith('饼'))
            return new("一个", 80);
        if (name.EndsWith('粥'))
            return new("一碗", 300);
        if (name.EndsWith('汤') || name.EndsWith('羹') || name.EndsWith("泡馍", StringComparison.Ordinal))
            return new("一碗", 300);
        if (name is "米饭" or "糙米饭")
            return new("一碗", 200);
        if (name.EndsWith('饭'))
            return new("一碗", 250);
        if (name is "面条")
            return new("一碗", 200);
        if (name.EndsWith('面'))
            return new("一碗", 300);
        if (name.EndsWith('粉'))
            return new("一碗", 400);
        if (name.EndsWith('杯'))
            return new("一杯", 80);
        if (name.Contains("豆浆", StringComparison.Ordinal))
            return new("一杯", 250);
        if (name.Contains("奶茶", StringComparison.Ordinal))
            return new("一杯", 350);
        if (name.Contains("双皮奶", StringComparison.Ordinal)
            || name.Contains("撞奶", StringComparison.Ordinal)
            || name.Contains("甘露", StringComparison.Ordinal)
            || name.EndsWith("豆沙", StringComparison.Ordinal))
            return new("一碗", 250);
        if (name.Contains("圣代", StringComparison.Ordinal)
            || name.Contains("甜筒", StringComparison.Ordinal)
            || name.Contains("花筒", StringComparison.Ordinal)
            || name.Contains("麦旋风", StringComparison.Ordinal))
            return new("一个", 120);
        if (name is "上校鸡块" or "劲爆鸡米花" or "麦乐鸡" or "薯条")
            return new("一份", 80);
        return null;
    }

    private static bool IsDish(string name)
    {
        if (name.Length < 3)
            return false;
        foreach (var mark in DishMarks)
        {
            if (name.Contains(mark, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static readonly string[] DishMarks =
    [
        "红烧", "糖醋", "宫保", "麻婆", "鱼香", "蒜蓉", "蒜香", "蒜泥", "蒜茸",
        "干锅", "干煸", "葱爆", "葱烧", "葱油", "清蒸", "清炖", "清炒", "家常",
        "醋溜", "焦溜", "油焖", "白灼", "凉拌", "白切", "回锅", "小炒", "孜然",
        "黑椒", "可乐", "啤酒", "锅包", "扣肉", "酥肉", "木须", "拍黄瓜", "佛跳墙",
        "酸辣", "酸菜", "肉丝", "肉片", "牛柳", "拉皮", "烧",
        "炒", "炖", "蒸", "煎", "焖", "爆", "煮", "卤", "拌", "溜", "炸", "烤", "扣",
    ];

    private static bool IsPowder(string name) =>
        name.Contains("淀粉", StringComparison.Ordinal)
        || name.EndsWith("奶粉", StringComparison.Ordinal)
        || name.EndsWith("蛋白粉", StringComparison.Ordinal)
        || name.EndsWith("蛋粉", StringComparison.Ordinal)
        || name.EndsWith("辣椒粉", StringComparison.Ordinal)
        || name.EndsWith("五香粉", StringComparison.Ordinal)
        || name.EndsWith("咖喱粉", StringComparison.Ordinal);

    private static FoodPortion? Cut(string name)
    {
        if (name.EndsWith('蹄'))
            return new("一只", 250);
        if (name.EndsWith('翅'))
            return new("一只", 45);
        if (name.EndsWith('腿'))
            return new("一只", 150);
        if (name.EndsWith('胸'))
            return new("一块", 150);
        if (name.EndsWith('爪') || name.EndsWith('掌'))
            return new("一只", 35);
        if (name.EndsWith('肝'))
            return new("一份", 50);
        if (name.EndsWith('心') || name.EndsWith('胗') || name.EndsWith('肾') || name.EndsWith("腰子", StringComparison.Ordinal))
            return new("一份", 40);
        if (name.EndsWith('肉') || name.EndsWith('排') || name.EndsWith('肺') || name.EndsWith('腰')
            || name.EndsWith('肚') || name.EndsWith('肠') || name.EndsWith('骨') || name.EndsWith('筋')
            || name.EndsWith('血') || name.EndsWith('舌') || (name.EndsWith('皮') && !name.EndsWith("豆皮", StringComparison.Ordinal)) || name.EndsWith('脑')
            || name.EndsWith('脯') || name.EndsWith('柳') || name.EndsWith('腩') || name.EndsWith('腱')
            || name.EndsWith('丝') || name.EndsWith('片') || name.EndsWith('馅') || name.EndsWith('丸')
            || name.EndsWith('松') || name.EndsWith('干') || name.EndsWith("排骨", StringComparison.Ordinal)
            || name is "肥牛" or "西冷" or "肉眼" or "米龙" or "T骨" or "培根")
            return new("一份", 100);
        return null;
    }

    private static Dictionary<string, FoodPortion> CreateExact()
    {
        var map = new Dictionary<string, FoodPortion>(StringComparer.Ordinal);
        void Add(string label, double amount, params string[] names)
        {
            var portion = new FoodPortion(label, amount);
            foreach (var name in names)
            {
                if (!map.TryAdd(name, portion))
                    throw new InvalidOperationException($"重复的份量：{name}");
            }
        }

        Add("一根", 200, "玉米");
        Add("一份", 40, "燕麦片");
        Add("一碗", 200, "面条");
        Add("一块", 150, "鸡胸肉", "鸡胸", "鸭胸", "豆腐", "冻豆腐");
        Add("一块", 120, "鸡腿肉", "三文鱼", "鳕鱼");
        Add("一块", 200, "内酯豆腐");
        Add("一块", 40, "豆腐干", "豆干");
        Add("一块", 50, "香干", "奶豆腐");
        Add("一块", 20, "油豆腐", "腐乳");
        Add("一张", 15, "豆腐皮");
        Add("一把", 40, "豆腐丝");
        Add("一把", 15, "腐竹");
        Add("一把", 150, "豆芽", "黄豆芽", "绿豆芽");
        Add("一盒", 40, "纳豆");
        Add("一勺", 10, "豆豉", "黄油", "猪油", "牛油", "鸡油");
        Add("一勺", 15, "炼乳", "奶油", "稀奶油");
        Add("一勺", 20, "淡奶");
        Add("一勺", 25, "奶粉", "全脂牛奶粉", "脱脂牛奶粉");
        Add("一勺", 30, "蛋白粉");
        Add("一片", 20, "奶酪");
        Add("一杯", 150, "酸奶", "希腊酸奶");
        Add("一个", 33, "鸡蛋白");
        Add("一个", 10, "鹌鹑蛋");
        Add("一个", 15, "鸽子蛋");
        Add("一个", 60, "皮蛋", "卤蛋");
        Add("一个", 70, "鸭蛋", "咸鸭蛋");
        Add("一个", 140, "鹅蛋");
        Add("一个", 3, "麻雀蛋");
        Add("一份", 100, "鸵鸟蛋", "虾仁");
        Add("一只", 50, "生蚝");
        Add("一只", 40, "鲍鱼");
        Add("一只", 15, "扇贝");
        Add("一份", 80, "豆渣");
        Add("一块", 30, "奶皮子");

        Add("一棵", 800, "大白菜");
        Add("一棵", 250, "娃娃菜");
        Add("一棵", 700, "甘蓝");
        Add("一棵", 600, "紫甘蓝");
        Add("一颗", 15, "抱子甘蓝");
        Add("一棵", 300, "西兰花");
        Add("一棵", 400, "菜花");
        Add("一棵", 200, "莴苣");
        Add("一把", 150,
            "小白菜", "菜心", "油菜", "鸡毛菜", "瓢儿白", "油麦菜", "生菜", "空心菜", "苋菜", "茼蒿",
            "菠菜", "韭菜", "韭黄", "芥菜", "豌豆苗", "香椿", "蕨菜", "马齿苋", "荠菜", "蒲公英",
            "枸杞叶", "清明菜", "沙参叶", "山苦荬叶", "食用黄麻", "汤菜", "紫背天葵", "苦苣", "苜蓿芽",
            "乳苣", "黄鹌菜", "猴腿蕨", "扫帚菜", "地皮菜", "刺嫩芽", "鱼腥草", "费菜", "米蒿", "马兰",
            "蒌蒿", "豆瓣菜", "风花菜", "球果蔊菜", "播娘蒿", "刺儿菜", "野苋菜", "芥蓝", "菜薹", "羽衣甘蓝");
        Add("一把", 10, "香菜", "薄荷", "罗勒", "迷迭香", "紫苏");
        Add("一把", 80, "蒜苗", "蒜薹");
        Add("一把", 50, "雪里蕻", "小根蒜");
        Add("一把", 200, "芹菜");
        Add("一把", 150, "水芹菜");
        Add("一根", 250, "莴笋", "西芹");
        Add("一根", 20, "芦笋");
        Add("一根", 120, "茭白", "胡萝卜", "牛蒡");
        Add("一根", 200, "竹笋", "山药");
        Add("一根", 100, "大葱");
        Add("一根", 15, "小葱", "葱", "香茅", "黄秋葵");
        Add("一根", 350, "萝卜");
        Add("一根", 250, "青萝卜", "木薯");
        Add("一根", 180, "黄瓜", "苦瓜");
        Add("一根", 200, "茄子", "西葫芦", "蛇瓜", "雪莲果");
        Add("一根", 150, "丝瓜");
        Add("一根", 250, "瓠瓜");
        Add("一个", 80, "青椒");
        Add("一个", 140, "彩椒");
        Add("一根", 20, "尖椒");
        Add("一根", 15, "辣椒");
        Add("一根", 120, "香蕉");
        Add("一个", 20, "樱桃萝卜", "荸荠");
        Add("一个", 120, "甜菜根", "人参果", "番石榴");
        Add("一个", 80, "桔梗", "菊芋", "芋头", "莲雾");
        Add("一个", 40, "慈姑", "海棠", "沙果", "无花果", "百香果", "李子");
        Add("一个", 15, "菱角", "金桔", "西梅");
        Add("一个", 150, "番茄");
        Add("一个", 200, "豆薯", "苹果", "梨", "芒果", "石榴");
        Add("一节", 200, "莲藕");
        Add("一个", 150, "土豆", "马铃薯", "洋葱", "桃", "柿子", "柑橘", "鳄梨", "蛋黄果");
        Add("一个", 200, "红薯", "甘薯", "紫薯", "春见耙耙柑", "丑橘");
        Add("一个", 300, "木瓜", "番木瓜", "火龙果");
        Add("一个", 600, "柚子");
        Add("一个", 90, "猕猴桃");
        Add("一个", 180, "橙子", "果冻橙", "武鸣沃柑", "赣南脐橙", "冰糖橙");
        Add("一个", 70, "柠檬");
        Add("一个", 130, "油桃");
        Add("一个", 100, "蟠桃", "杨桃");
        Add("一个", 35, "杏");
        Add("一个", 20, "梅子");
        Add("一个", 30, "枇杷", "海棠果");
        Add("一个", 10, "山楂");
        Add("一个", 50, "沙糖桔");
        Add("一个", 60, "山竹");
        Add("一个", 250, "佛手瓜", "释迦果");
        Add("一个", 80, "温柏", "椰色果");
        Add("一个", 40, "欧楂");
        Add("一颗", 12, "圣女果");
        Add("一颗", 8, "樱桃", "白玉樱桃", "黑枣");
        Add("一颗", 15, "荔枝", "红毛丹");
        Add("一颗", 10, "龙眼", "枣");
        Add("一颗", 20, "蛇皮果");
        Add("一颗", 5, "橄榄", "槟榔");
        Add("一串", 150, "葡萄", "提子");
        Add("一把", 100, "草莓", "桑葚", "杨梅");
        Add("一把", 50, "蓝莓", "树莓", "黑莓", "覆盆子", "罗甘莓");
        Add("一把", 40, "云莓", "拐枣");
        Add("一把", 30, "沙枣", "花楸果");
        Add("一把", 20, "蔷薇果", "益智果", "黄花菜");
        Add("一把", 100, "荷兰豆", "莼菜");
        Add("一把", 150, "四季豆");
        Add("一把", 120, "扁豆");
        Add("一把", 80, "毛豆");
        Add("一把", 100, "刀豆");
        Add("一块", 400, "冬瓜");
        Add("一块", 300, "南瓜");
        Add("一块", 200, "魔芋", "苤蓝");
        Add("一块", 25, "榨菜");
        Add("一块", 30, "沙蓬子");
        Add("一头", 40, "蒜");
        Add("一块", 20, "姜");
        Add("一片", 300, "西瓜");
        Add("一片", 200, "哈密瓜");
        Add("一片", 150, "甜瓜");
        Add("一片", 100, "菠萝");
        Add("一瓣", 80, "菠萝蜜");
        Add("一瓣", 60, "榴莲");
        Add("一节", 100, "甘蔗");

        Add("一把", 100,
            "香菇", "平菇", "金针菇", "杏鲍菇", "草菇", "茶树菇", "猴头菇", "松茸", "松蘑",
            "秀珍菇", "滑子菇", "姬松茸", "白灵菇", "榆黄蘑", "鸡腿菇", "真姬菇", "榛蘑",
            "鲍鱼菇", "灰树花", "鸡枞", "虎掌菌", "老人头菇", "鸡油菌", "元蘑", "牛肝菌", "红菇");
        Add("一把", 10, "口蘑", "竹荪", "羊肚菌", "黑木耳", "银耳", "紫菜", "花菇", "浒苔");
        Add("一小片", 5, "灵芝");
        Add("一份", 10, "松露", "冬虫夏草");
        Add("一份", 15, "乌鱼子");
        Add("一条", 30, "泥鳅");
        Add("一只", 150, "牛蛙");
        Add("一只", 400, "乌骨鸡", "清远鸡");
        Add("一块", 80, "鸡屎藤粑仔");
        Add("一碗", 250, "椰奶清补凉");
        Add("一把", 50, "海带", "裙带菜", "石花菜", "马尾藻", "海茸", "鹿角菜", "江蓠", "海萝", "礁膜", "蕨藻");

        Add("一个", 12, "栗子");
        Add("一把", 30, "白果", "银杏");
        Add("一块", 40, "可可椰子");
        Add("一个", 30, "南翔小笼馒头");
        Add("一个", 50, "狗不理包子");
        Add("一套", 200, "煎饼果子");
        Add("一块", 60, "驴打滚");
        Add("一个", 50, "艾窝窝");
        Add("一块", 80, "豌豆黄");
        Add("一碗", 250, "炒肝");
        Add("一个", 25, "焦圈");
        Add("一根", 40, "棒槌果子");
        Add("一碗", 300, "嘎巴菜", "糁");
        Add("一盘", 250, "贴饽饽熬小鱼");
        Add("一个", 40, "蟹壳黄");
        Add("一份", 250, "豆皮");
        Add("一碗", 300, "扁肉");
        Add("一个", 200, "糯米鸡");
        Add("一份", 150, "西多士");
        Add("一份", 120, "咖喱鱼蛋");
        Add("一个", 80, "菠萝油");
        Add("一块", 80, "烤麸", "年糕", "糍粑");
        Add("一份", 50, "面筋");
        Add("一盘", 200, "凉皮");
        Add("一把", 30, "锅巴", "爆米花");
        Add("一片", 10, "米饼");
        Add("一只", 30, "鸡爪", "鸡翅根");
        Add("一只", 35, "鸡翅中");
        Add("一只", 40, "鸡翅");
        Add("一只", 150, "鸡腿", "鸭腿", "鸽子", "鸽肉");
        Add("一只", 80, "鹌鹑");
        Add("一只", 250, "猪蹄");
        Add("一根", 150, "猪尾巴");
        Add("一只", 80, "猪耳");
        Add("一块", 120, "吮指原味鸡");
        Add("一个", 220, "巨无霸");
        Add("一小碟", 30, "老醋花生米");
        Add("一盘", 150, "拍黄瓜", "老虎菜", "皮蛋豆腐", "凉拌豆腐");
        Add("一碗", 300, "福州佛跳墙");
        Add("一份", 30, "金华火腿");
        Add("一个", 200, "红烧兔头");

        return map;
    }

    private static readonly HashSet<string> DryGrains = new(StringComparer.Ordinal)
    {
        "粳米", "籼米", "糯米", "黑米", "紫米", "红米", "糙米", "小米", "黄米", "高粱", "小麦", "大麦",
        "燕麦", "荞麦", "藜麦", "青稞", "黍米", "稷米", "薏米", "玉米面", "小麦粉", "全麦粉", "大米粉",
        "糯米粉",         "挂面", "米粉", "米线", "粉丝", "粉条", "黑麦", "小黑麦", "糜子", "粟", "黍",
    };

    private static readonly HashSet<string> DryBeans = new(StringComparer.Ordinal)
    {
        "黄豆", "黑豆", "青豆", "红豆", "绿豆", "白扁豆", "赤小豆", "芸豆", "豌豆", "蚕豆", "鹰嘴豆", "豇豆", "饭豇豆",
    };

    private static readonly HashSet<string> Seeds = new(StringComparer.Ordinal)
    {
        "芝麻", "黑芝麻", "亚麻籽", "奇亚籽",
    };

    private static readonly HashSet<string> Nuts = new(StringComparer.Ordinal)
    {
        "花生", "杏仁", "核桃", "腰果", "瓜子", "葵花子", "南瓜子", "榛子", "松子", "开心果",
        "夏威夷果", "碧根果", "巴旦木", "巴西坚果", "胡桃", "印加果", "西瓜籽", "茶籽",
    };

    private static readonly HashSet<string> DrySpices = new(StringComparer.Ordinal)
    {
        "花椒", "八角", "桂皮", "香叶", "丁香", "孜然", "茴香", "胡椒", "芥末",
        "砂仁", "沙姜", "肉豆蔻", "甘草", "罗汉果", "陈皮", "橙叶", "山奈", "白芷", "良姜", "木香", "荜拨", "香果", "排草", "灵草",
    };
}
