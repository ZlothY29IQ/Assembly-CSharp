using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class CosmeticsLegacyV1Info
{
	public const int k_bodyDockPositions_allObjects_length = 224;

	private static readonly Dictionary<string, string> k_special = new Dictionary<string, string> { { "Slingshot", "Slingshot" } };

	private static readonly Dictionary<string, string> k_packs = new Dictionary<string, string>
	{
		{ "TUXEDO SET", "LSAAO." },
		{ "EXPLORER SET", "LSAAN." },
		{ "SANTA SET 22", "LSAAP." },
		{ "SNOWMAN SET", "LSAAQ." },
		{ "EVIL SANTA SET", "LSAAR." },
		{ "Day 1 Pack", "LSAAP2." },
		{ "DAY 1 PACK", "LSAAP2." },
		{ "LAUNCH BUNDLE", "LSAAP2." },
		{ "LSAAP.2. (1)", "LSAAP2." },
		{ "POLAR BEAR SET", "LSAAT." },
		{ "WIZARD SET", "LSAAV." },
		{ "KNIGHT SET", "LSAAW." },
		{ "BARBARIAN SET", "LSAAX." },
		{ "ORC SET", "LSAAY." },
		{ "LSAAS.", "LSAAS." },
		{ "LSAAU.", "LSAAU." },
		{ "MERFOLK SET", "LSAAZ." },
		{ "SCUBA SET", "LSABA." },
		{ "SAFARI SET", "LSABB." },
		{ "CRYSTAL CAVERNS SET", "LSABC." },
		{ "SPIDER MONKE PACK", "LSABD." },
		{ "HOLIDAY FIR PACK", "LSABE." },
		{ "MAD SCIENTIST PACK", "LSABF." },
		{ "I LAVA YOU PACK", "LSABG." },
		{ "BEEKEEPER PACK", "LSABH." },
		{ "LEAF NINJA PACK", "LSABJ." },
		{ "MONKE MONK PACK", "LSABK." },
		{ "GLAM ROCKER PACK", "LSABL." }
	};

	private static readonly Dictionary<string, string> k_oldPacks = new Dictionary<string, string>
	{
		{ "CLOWN SET", "CLOWN SET" },
		{ "VAMPIRE SET", "VAMPIRE SET" },
		{ "WEREWOLF SET", "WEREWOLF SET" },
		{ "STAR PRINCESS SET", "STAR PRINCESS SET" },
		{ "SANTA SET", "SANTA SET" },
		{ "CARDBOARD ARMOR SET", "CARDBOARD ARMOR SET" },
		{ "SPIKED ARMOR SET", "SPIKED ARMOR SET" },
		{ "GORILLA ARMOR SET", "GORILLA ARMOR SET" },
		{ "SHERIFF SET", "SHERIFF SET" },
		{ "ROBOT SET", "ROBOT SET" },
		{ "CLOWN 22 SET", "CLOWN 22 SET" },
		{ "SUPER HERO SET", "SUPER HERO SET" },
		{ "UNICORN PRINCESS SET", "UNICORN PRINCESS SET" }
	};

	private static readonly Dictionary<string, string> k_unused = new Dictionary<string, string>
	{
		{ "HIGH TECH SLINGSHOT", "HIGH TECH SLINGSHOT" },
		{ "THROWABLE SQUISHY EYEBALL", "THROWABLE SQUISHY EYEBALL" }
	};

	private static readonly Dictionary<string, string> k_v1DisplayNames_to_playFabIds = new Dictionary<string, string>
	{
		{ "TREE PIN", "LBAAA." },
		{ "BOWTIE", "LBAAB." },
		{ "BASIC SCARF", "LBAAC." },
		{ "ADMINISTRATOR BADGE", "LBAAD." },
		{ "EARLY ACCESS", "LBAAE." },
		{ "CRYSTALS PIN", "LBAAF." },
		{ "CANYON PIN", "LBAAG." },
		{ "CITY PIN", "LBAAH." },
		{ "GORILLA PIN", "LBAAI." },
		{ "NECK SCARF", "LBAAJ." },
		{ "MOD STICK", "LBAAK." },
		{ "CLOWN FRILL", "LBAAL." },
		{ "VAMPIRE COLLAR", "LBAAM." },
		{ "WEREWOLF CLAWS", "LBAAN." },
		{ "STAR PRINCESS WAND", "LBAAO." },
		{ "TURKEY LEG", "LBAAP." },
		{ "TURKEY FINGER PUPPET", "LBAAQ." },
		{ "CANDY CANE", "LBAAR." },
		{ "SPARKLER", "LBAAS." },
		{ "ICICLE", "LBAAT." },
		{ "CHEST HEART", "LBAAU." },
		{ "RED ROSE", "LBAAV." },
		{ "PINK ROSE", "LBAAW." },
		{ "BLACK ROSE", "LBAAX." },
		{ "GOLD ROSE", "LBAAY." },
		{ "GT1 BADGE", "LBAAZ." },
		{ "THUMB PARTYHATS", "LBABA." },
		{ "REGULAR WRENCH", "LBABB." },
		{ "GOLD WRENCH", "LBABC." },
		{ "REGULAR FORK AND KNIFE", "LBABD." },
		{ "GOLD FORK AND KNIFE", "LBABE." },
		{ "FOUR LEAF CLOVER", "LBABF." },
		{ "GOLDEN FOUR LEAF CLOVER", "LBABG." },
		{ "MOUNTAIN PIN", "LBABH." },
		{ "YELLOW RAIN SHAWL", "LBABI." },
		{ "POCKET GORILLA BUN YELLOW", "LBABJ." },
		{ "POCKET GORILLA BUN BLUE", "LBABK." },
		{ "POCKET GORILLA BUN PINK", "LBABL." },
		{ "BONGOS", "LBABM." },
		{ "DRUM SET", "LBABN." },
		{ "SPILLED ICE CREAM", "LBABO." },
		{ "FLAMINGO FLOATIE", "LBABP." },
		{ "PAINTBALL SNOW VEST", "LBABQ." },
		{ "PAINTBALL FOREST VEST", "LBABR." },
		{ "CARDBOARD ARMOR", "LBABS." },
		{ "GORILLA ARMOR", "LBABT." },
		{ "SPIKED ARMOR", "LBABU." },
		{ "CLOWN VEST", "LBABV." },
		{ "ROBOT BODY", "LBABW." },
		{ "SHERIFF VEST", "LBABX." },
		{ "SUPER HERO BODY", "LBABY." },
		{ "UNICORN TUTU", "LBABZ." },
		{ "BIG EYEBROWS", "LFAAA." },
		{ "NOSE RING", "LFAAB." },
		{ "BASIC EARRINGS", "LFAAC." },
		{ "TRIPLE EARRINGS", "LFAAD." },
		{ "EYEBROW STUD", "LFAAE." },
		{ "TRIANGLE SUNGLASSES", "LFAAF." },
		{ "SKULL MASK", "LFAAG." },
		{ "RIGHT EYEPATCH", "LFAAH." },
		{ "LEFT EYEPATCH", "LFAAI." },
		{ "DOUBLE EYEPATCH", "LFAAJ." },
		{ "GOGGLES", "LFAAK." },
		{ "SURGICAL MASK", "LFAAL." },
		{ "TORTOISESHELL SUNGLASSES", "LFAAM." },
		{ "AVIATORS", "LFAAN." },
		{ "ROUND SUNGLASSES", "LFAAO." },
		{ "WITCH NOSE", "LFAAP." },
		{ "MUMMY WRAP", "LFAAQ." },
		{ "CLOWN NOSE", "LFAAR." },
		{ "VAMPIRE FANGS", "LFAAS." },
		{ "WEREWOLF FACE", "LFAAT." },
		{ "STAR PRINCESS GLASSES", "LFAAU." },
		{ "MAPLE LEAF", "LFAAV." },
		{ "FACE SCARF", "LFAAW." },
		{ "SANTA BEARD", "LFAAX." },
		{ "ORNAMENT EARRINGS", "LFAAY." },
		{ "2022 GLASSES", "LFAAZ." },
		{ "NOSE SNOWFLAKE", "LFABA." },
		{ "ROSY CHEEKS", "LFABB." },
		{ "BOXY SUNGLASSES", "LFABC." },
		{ "HEART GLASSES", "LFABD." },
		{ "COOKIE JAR", "LFABE." },
		{ "BITE ONION", "LFABF." },
		{ "EMPEROR NOSE BUTTERFLY", "LFABG." },
		{ "FOREHEAD EGG", "LFABH." },
		{ "LIGHTNING MAKEUP", "LFABI." },
		{ "BLUE SHUTTERS", "LFABJ." },
		{ "BLACK SHUTTERS", "LFABK." },
		{ "GREEN SHUTTERS", "LFABL." },
		{ "RED SHUTTERS", "LFABM." },
		{ "SUNBURN", "LFABN." },
		{ "SUNSCREEN", "LFABO." },
		{ "PAINTBALL FOREST VISOR", "LFABP." },
		{ "PAINTBALL SNOW VISOR", "LFABQ." },
		{ "PAINTBALL GORILLA VISOR", "LFABR." },
		{ "BULGING GOOGLY EYES", "LFABS." },
		{ "CLOWN NOSE 22", "LFABT." },
		{ "SHERIFF MUSTACHE", "LFABU." },
		{ "SLINKY EYES", "LFABV." },
		{ "MOUTH WHEAT", "LFABW." },
		{ "BANANA HAT", "LHAAA." },
		{ "CAT EARS", "LHAAB." },
		{ "PARTY HAT", "LHAAC." },
		{ "USHANKA", "LHAAD." },
		{ "SWEATBAND", "LHAAE." },
		{ "BASEBALL CAP", "LHAAF." },
		{ "GOLDEN HEAD", "LHAAG." },
		{ "FOREHEAD MIRROR", "LHAAH." },
		{ "PINEAPPLE HAT", "LHAAI." },
		{ "WITCH HAT", "LHAAJ." },
		{ "COCONUT", "LHAAK." },
		{ "SUNHAT", "LHAAL." },
		{ "CLOCHE", "LHAAM." },
		{ "COWBOY HAT", "LHAAN." },
		{ "FEZ", "LHAAO." },
		{ "TOP HAT", "LHAAP." },
		{ "BASIC BEANIE", "LHAAQ." },
		{ "WHITE FEDORA", "LHAAR." },
		{ "FLOWER CROWN", "LHAAS." },
		{ "PAPERBAG HAT", "LHAAT." },
		{ "PUMPKIN HAT", "LHAAU." },
		{ "CLOWN WIG", "LHAAV." },
		{ "VAMPIRE WIG", "LHAAW." },
		{ "WEREWOLF EARS", "LHAAX." },
		{ "STAR PRINCESS TIARA", "LHAAY." },
		{ "PIRATE BANDANA", "LHAAZ." },
		{ "SUNNY SUNHAT", "LHABA." },
		{ "CHROME COWBOY HAT", "LHABB." },
		{ "CHEFS HAT", "LHABC." },
		{ "SANTA HAT", "LHABD." },
		{ "SNOWMAN HAT", "LHABE." },
		{ "GIFT HAT", "LHABF." },
		{ "ELF HAT", "LHABG." },
		{ "ORANGE POMPOM HAT", "LHABH." },
		{ "BLUE POMPOM HAT", "LHABI." },
		{ "STRIPE POMPOM HAT", "LHABJ." },
		{ "PATTERN POMPOM HAT", "LHABK." },
		{ "WHITE EARMUFFS", "LHABL." },
		{ "BLACK EARMUFFS", "LHABM." },
		{ "GREEN EARMUFFS", "LHABN." },
		{ "PINK EARMUFFS", "LHABO." },
		{ "HEADPHONES1", "LHABP." },
		{ "BOX OF CHOCOLATES HAT", "LHABQ." },
		{ "HEART POMPOM HAT", "LHABR." },
		{ "PLUNGER HAT", "LHABS." },
		{ "SAUCEPAN HAT", "LHABT." },
		{ "WHITE BUNNY EARS", "LHABU." },
		{ "BROWN BUNNY EARS", "LHABV." },
		{ "LEPRECHAUN HAT", "LHABW." },
		{ "BLUE LILY HAT", "LHABX." },
		{ "PURPLE LILY HAT", "LHABY." },
		{ "YELLOW RAIN HAT", "LHABZ." },
		{ "PAINTED EGG HAT", "LHACA." },
		{ "BLACK LONGHAIR WIG", "LHACB." },
		{ "RED LONGHAIR WIG", "LHACC." },
		{ "ELECTRO HELM", "LHACD." },
		{ "SEAGULL", "LHACE." },
		{ "ROCKIN MOHAWK", "LHACF." },
		{ "SPIKED HELMET", "LHACG." },
		{ "CARDBOARD HELMET", "LHACH." },
		{ "CLOWN CAP", "LHACI." },
		{ "PUMPKIN HEAD HAPPY", "LHACJ." },
		{ "PUMPKIN HEAD SCARY", "LHACK." },
		{ "ROBOT HEAD", "LHACL." },
		{ "SHERIFF HAT", "LHACM." },
		{ "UNICORN CROWN", "LHACN." },
		{ "SUPER HERO HEADBAND", "LHACO." },
		{ "PIE HAT", "LHACP." },
		{ "SCARECROW HAT", "LHACQ." },
		{ "CHERRY BLOSSOM BRANCH", "LMAAA." },
		{ "CHERRY BLOSSOM BRANCH ROSE GOLD", "LMAAB." },
		{ "YELLOW HAND BOOTS", "LMAAC." },
		{ "CLOUD HAND BOOTS", "LMAAD." },
		{ "GOLDEN HAND BOOTS", "LMAAE." },
		{ "BLACK UMBRELLA", "LMAAF." },
		{ "COLORFUL UMBRELLA", "LMAAG." },
		{ "GOLDEN UMBRELLA", "LMAAH." },
		{ "ACOUSTIC GUITAR", "LMAAI." },
		{ "GOLDEN ACOUSTIC GUITAR", "LMAAJ." },
		{ "ELECTRIC GUITAR", "LMAAK." },
		{ "GOLDEN ELECTRIC GUITAR", "LMAAL." },
		{ "BUBBLER", "LMAAM." },
		{ "POPSICLE", "LMAAN." },
		{ "RUBBER DUCK", "LMAAO." },
		{ "STAR BALLOON", "LMAAP." },
		{ "STAR BALLON", "LMAAP." },
		{ "STICKABLE TAR.GET", "LMAAQ." },
		{ "STICKABLE TARGET", "LMAAQ." },
		{ "DIAMOND BALLOON", "LMAAR." },
		{ "CHOCOLATE DONUT BALLOON", "LMAAS." },
		{ "HEART BALLOON", "LMAAT." },
		{ "FINGER FLAG", "LMAAU." },
		{ "HIGH TECH S.LINGSHOT", "LMAAV." },
		{ "UNICORN STAFF", "LMAAW." },
		{ "GHOST BALLOON", "LMAAX." },
		{ "GIANT CANDY BAR", "LMAAY." },
		{ "CANDY BAR FUN SIZE", "LMAAZ." },
		{ "SPIDER WEB UMBRELLA", "LMABA." },
		{ "DEADSHOT", "LMABB." },
		{ "YORICK", "LMABC." },
		{ "PINK DONUT BALLOON", "LMABD." },
		{ "TURKEY TOY", "LMABE." },
		{ "CRANBERRY CAN", "LMABF." },
		{ "FRYING PAN", "LMABG." },
		{ "BALLOON TURKEY", "LMABH." },
		{ "CANDY APPLE", "LMABI." },
		{ "CARAMEL APPLE", "LMABJ." },
		{ "PIE SLICE", "LMABK." },
		{ "LADLE", "LMABL." },
		{ "TURKEY LEG 22", "LMABM." },
		{ "CORN ON THE COB", "LMABN." },
		{ "FINGER OLIVES", "LMABO." }
	};

	private static readonly Dictionary<string, int[]> _k_playFabId_to_bodyDockPositions_allObjects_indexes = new Dictionary<string, int[]>
	{
		{
			"LMAAC.",
			new int[2] { 0, 1 }
		},
		{
			"LMAAD.",
			new int[2] { 2, 3 }
		},
		{
			"LMAAE.",
			new int[2] { 4, 5 }
		},
		{
			"LMAAK.",
			new int[2] { 6, 7 }
		},
		{
			"LMAAL.",
			new int[2] { 8, 9 }
		},
		{
			"LMAAF.",
			new int[1] { 10 }
		},
		{
			"LBABE.",
			new int[2] { 11, 12 }
		},
		{
			"LBABD.",
			new int[2] { 13, 14 }
		},
		{
			"LMAAG.",
			new int[1] { 15 }
		},
		{
			"LMAAH.",
			new int[1] { 16 }
		},
		{
			"LMAAO.",
			new int[1] { 17 }
		},
		{
			"LBABB.",
			new int[1] { 18 }
		},
		{
			"LBAAP.",
			new int[1] { 19 }
		},
		{
			"LBAAS.",
			new int[1] { 20 }
		},
		{
			"LBAAT.",
			new int[1] { 21 }
		},
		{
			"LBAAY.",
			new int[1] { 22 }
		},
		{
			"LBABC.",
			new int[1] { 23 }
		},
		{
			"LBAAW.",
			new int[1] { 24 }
		},
		{
			"LBAAV.",
			new int[1] { 25 }
		},
		{
			"LBABF.",
			new int[1] { 26 }
		},
		{
			"LBAAX.",
			new int[1] { 27 }
		},
		{
			"LBAAK.",
			new int[1] { 28 }
		},
		{
			"LBABG.",
			new int[1] { 29 }
		},
		{
			"LBAAO.",
			new int[1] { 30 }
		},
		{
			"LMAAA.",
			new int[1] { 31 }
		},
		{
			"LMAAB.",
			new int[1] { 32 }
		},
		{
			"LMAAM.",
			new int[1] { 33 }
		},
		{
			"LMAAN.",
			new int[1] { 34 }
		},
		{
			"LBAAR.",
			new int[1] { 35 }
		},
		{
			"LMAAQ.",
			new int[1] { 36 }
		},
		{
			"LMAAP.",
			new int[1] { 37 }
		},
		{
			"LMAAR.",
			new int[1] { 38 }
		},
		{
			"LMAAS.",
			new int[1] { 39 }
		},
		{
			"LMABD.",
			new int[1] { 40 }
		},
		{
			"LMAAT.",
			new int[1] { 41 }
		},
		{
			"LMABA.",
			new int[1] { 42 }
		},
		{
			"LMAAW.",
			new int[1] { 43 }
		},
		{
			"LMAAX.",
			new int[1] { 44 }
		},
		{
			"LMAAY.",
			new int[1] { 45 }
		},
		{
			"LMAAZ.",
			new int[1] { 46 }
		},
		{
			"LMABC.",
			new int[1] { 47 }
		},
		{
			"LMABE.",
			new int[1] { 48 }
		},
		{
			"LMABF.",
			new int[1] { 49 }
		},
		{
			"LMABI.",
			new int[1] { 50 }
		},
		{
			"LMABJ.",
			new int[1] { 51 }
		},
		{
			"LMABH.",
			new int[1] { 52 }
		},
		{
			"LMABG.",
			new int[1] { 53 }
		},
		{
			"LMABL.",
			new int[1] { 54 }
		},
		{
			"LMABM.",
			new int[1] { 55 }
		},
		{
			"LMABK.",
			new int[1] { 56 }
		},
		{
			"LMABN.",
			new int[1] { 57 }
		},
		{
			"LMABS.",
			new int[1] { 58 }
		},
		{
			"LMABR.",
			new int[1] { 59 }
		},
		{
			"LMABT.",
			new int[1] { 60 }
		},
		{
			"LMABP.",
			new int[1] { 61 }
		},
		{
			"LMABQ.",
			new int[1] { 62 }
		},
		{
			"LMABU.",
			new int[1] { 63 }
		},
		{
			"LMABW.",
			new int[1] { 64 }
		},
		{
			"LMABX.",
			new int[1] { 65 }
		},
		{
			"LMACB.",
			new int[1] { 66 }
		},
		{
			"LMACC.",
			new int[1] { 67 }
		},
		{
			"LMACD.",
			new int[1] { 68 }
		},
		{
			"LMACI.",
			new int[1] { 69 }
		},
		{
			"LMACJ.",
			new int[1] { 70 }
		},
		{
			"LMACL.",
			new int[1] { 71 }
		},
		{
			"LMACR.",
			new int[1] { 72 }
		},
		{
			"LMACQ.",
			new int[1] { 73 }
		},
		{
			"LMACS.",
			new int[1] { 74 }
		},
		{
			"LMACP.",
			new int[1] { 75 }
		},
		{
			"LMACT.",
			new int[1] { 76 }
		},
		{
			"LMACV.",
			new int[1] { 77 }
		},
		{
			"LMACW.",
			new int[1] { 78 }
		},
		{
			"LMACY.",
			new int[1] { 79 }
		},
		{
			"LMADA.",
			new int[1] { 80 }
		},
		{
			"LMADB.",
			new int[1] { 81 }
		},
		{
			"LMADD.",
			new int[1] { 82 }
		},
		{
			"LMADE.",
			new int[1] { 83 }
		},
		{
			"LMADH.",
			new int[1] { 84 }
		},
		{
			"LMADJ.",
			new int[1] { 85 }
		},
		{
			"LMADK.",
			new int[1] { 86 }
		},
		{
			"LMADL.",
			new int[1] { 87 }
		},
		{
			"LMADM.",
			new int[1] { 88 }
		},
		{
			"LMADN.",
			new int[1] { 89 }
		},
		{
			"LMADQ.",
			new int[1] { 90 }
		},
		{
			"LMADR.",
			new int[1] { 91 }
		},
		{
			"LMADS.",
			new int[1] { 92 }
		},
		{
			"LMADV.",
			new int[1] { 93 }
		},
		{
			"LMADW.",
			new int[1] { 94 }
		},
		{
			"LMADX.",
			new int[1] { 95 }
		},
		{
			"LMADZ.",
			new int[1] { 96 }
		},
		{
			"LMAEA.",
			new int[1] { 97 }
		},
		{
			"LMAEB.",
			new int[1] { 98 }
		},
		{
			"LMAEC.",
			new int[1] { 99 }
		},
		{
			"LMAED.",
			new int[1] { 100 }
		},
		{
			"LMAEF.",
			new int[1] { 101 }
		},
		{
			"LMAEG.",
			new int[1] { 102 }
		},
		{
			"LMAEH.",
			new int[1] { 103 }
		},
		{
			"LMADY.",
			new int[1] { 104 }
		},
		{
			"LMAEK.",
			new int[1] { 105 }
		},
		{
			"LMAEL.",
			new int[1] { 106 }
		},
		{
			"LMAEM.",
			new int[1] { 107 }
		},
		{
			"LMAEN.",
			new int[1] { 108 }
		},
		{
			"LMAEP.",
			new int[1] { 109 }
		},
		{
			"LMAEQ.",
			new int[1] { 110 }
		},
		{
			"LMAES.",
			new int[1] { 111 }
		},
		{
			"LMAEU.",
			new int[1] { 112 }
		},
		{
			"LMAER.",
			new int[1] { 113 }
		},
		{
			"LMAET.",
			new int[1] { 114 }
		},
		{
			"LMAFH.",
			new int[1] { 115 }
		},
		{
			"LMAFA.",
			new int[1] { 116 }
		},
		{
			"LMAFB.",
			new int[1] { 117 }
		},
		{
			"LMAFC.",
			new int[1] { 118 }
		},
		{
			"LMAFD.",
			new int[1] { 119 }
		},
		{
			"LMAFE.",
			new int[1] { 120 }
		},
		{
			"LMAFF.",
			new int[1] { 121 }
		},
		{
			"LMAFI.",
			new int[1] { 122 }
		},
		{
			"LMAFG.",
			new int[1] { 123 }
		},
		{
			"LMAFJ.",
			new int[1] { 124 }
		},
		{
			"LMAFL.",
			new int[1] { 125 }
		},
		{
			"LMAFM.",
			new int[1] { 126 }
		},
		{
			"LMAFO.",
			new int[1] { 127 }
		},
		{
			"LMAFP.",
			new int[1] { 128 }
		},
		{
			"LMAFR.",
			new int[1] { 129 }
		},
		{
			"LMAFS.",
			new int[1] { 130 }
		},
		{
			"LMAFQ.",
			new int[1] { 131 }
		},
		{
			"LMAFT.",
			new int[1] { 132 }
		},
		{
			"LMAFU.",
			new int[1] { 133 }
		},
		{
			"LMAFV.",
			new int[1] { 134 }
		},
		{
			"LMAFW.",
			new int[1] { 135 }
		},
		{
			"LMAFZ.",
			new int[1] { 136 }
		},
		{
			"LMAGA.",
			new int[1] { 137 }
		},
		{
			"LMAGC.",
			new int[1] { 138 }
		},
		{
			"LMAGB.",
			new int[1] { 139 }
		},
		{
			"LMAGF.",
			new int[1] { 140 }
		},
		{
			"LMAGG.",
			new int[1] { 141 }
		},
		{
			"LMAGI.",
			new int[1] { 142 }
		},
		{
			"LMAGK.",
			new int[1] { 143 }
		},
		{
			"LMAGL.",
			new int[1] { 144 }
		},
		{
			"LMAGN.",
			new int[1] { 145 }
		},
		{
			"LMAGO.",
			new int[1] { 146 }
		},
		{
			"LMAGQ.",
			new int[1] { 147 }
		},
		{
			"LMAGZ.",
			new int[1] { 148 }
		},
		{
			"LMAGS.",
			new int[1] { 149 }
		},
		{
			"LMAGV.",
			new int[1] { 150 }
		},
		{
			"LMAGW.",
			new int[1] { 151 }
		},
		{
			"LMAGY.",
			new int[1] { 152 }
		},
		{
			"LMAHA.",
			new int[1] { 153 }
		},
		{
			"LMAHB.",
			new int[1] { 154 }
		},
		{
			"LMAHD.",
			new int[1] { 155 }
		},
		{
			"LMAHE.",
			new int[1] { 156 }
		},
		{
			"LMAHF.",
			new int[1] { 157 }
		},
		{
			"LMAHG.",
			new int[1] { 158 }
		},
		{
			"LMAHI.",
			new int[1] { 159 }
		},
		{
			"LMAHJ.",
			new int[1] { 160 }
		},
		{
			"LMAHK.",
			new int[1] { 161 }
		},
		{
			"LMAHO.",
			new int[1] { 162 }
		},
		{
			"LMAHM.",
			new int[1] { 163 }
		},
		{
			"LMAHN.",
			new int[1] { 164 }
		},
		{
			"LMAHP.",
			new int[1] { 165 }
		},
		{
			"LMAHS.",
			new int[1] { 166 }
		},
		{
			"LMAHT.",
			new int[1] { 167 }
		},
		{
			"LMAHU.",
			new int[1] { 168 }
		},
		{
			"LMAHV.",
			new int[1] { 169 }
		},
		{
			"LMAHZ.",
			new int[1] { 170 }
		},
		{
			"LMAIA.",
			new int[1] { 171 }
		},
		{
			"LMAHW.",
			new int[1] { 172 }
		},
		{
			"LMAHY.",
			new int[1] { 173 }
		},
		{
			"LMAHX.",
			new int[1] { 174 }
		},
		{
			"LMAII.",
			new int[1] { 175 }
		},
		{
			"LMAIH.",
			new int[1] { 176 }
		},
		{
			"LMAIJ.",
			new int[1] { 177 }
		},
		{
			"LMAIK.",
			new int[1] { 178 }
		},
		{
			"LMAIL.",
			new int[1] { 179 }
		},
		{
			"LMAIN.",
			new int[1] { 180 }
		},
		{
			"LMAIQ.",
			new int[1] { 181 }
		},
		{
			"LMAIS.",
			new int[1] { 182 }
		},
		{
			"LMAIT.",
			new int[1] { 183 }
		},
		{
			"LMAIU.",
			new int[1] { 184 }
		},
		{
			"LMAIX.",
			new int[1] { 185 }
		},
		{
			"LMAIW.",
			new int[1] { 186 }
		},
		{
			"LMAIV.",
			new int[1] { 187 }
		},
		{
			"LMAIY.",
			new int[1] { 188 }
		},
		{
			"LMAIZ.",
			new int[1] { 189 }
		},
		{
			"LMAJA.",
			new int[1] { 190 }
		},
		{
			"LMAJB.",
			new int[1] { 191 }
		},
		{
			"LMAJC.",
			new int[1] { 192 }
		},
		{
			"LMAJD.",
			new int[1] { 193 }
		},
		{
			"LMAJE.",
			new int[1] { 194 }
		},
		{
			"LMAJF.",
			new int[1] { 195 }
		},
		{
			"LMAJH.",
			new int[1] { 196 }
		},
		{
			"LMAJI.",
			new int[1] { 197 }
		},
		{
			"LMAJJ.",
			new int[1] { 198 }
		},
		{
			"LMAJN.",
			new int[1] { 199 }
		},
		{
			"LMAJK.",
			new int[1] { 200 }
		},
		{
			"LMAJL.",
			new int[1] { 201 }
		},
		{
			"LMAJM.",
			new int[1] { 202 }
		},
		{
			"LMAJS.",
			new int[1] { 203 }
		},
		{
			"LMAJT.",
			new int[1] { 204 }
		},
		{
			"LMAJU.",
			new int[1] { 205 }
		},
		{
			"LMAJW.",
			new int[1] { 206 }
		},
		{
			"LMAJX.",
			new int[1] { 207 }
		},
		{
			"LMAJZ.",
			new int[1] { 208 }
		},
		{
			"LMAKA.",
			new int[1] { 209 }
		},
		{
			"LMAKB.",
			new int[1] { 210 }
		},
		{
			"LMAJV.",
			new int[1] { 211 }
		},
		{
			"Slingshot",
			new int[1] { 212 }
		},
		{
			"HIGH TECH SLINGSHOT",
			new int[1] { 213 }
		},
		{
			"LMABB.",
			new int[1] { 214 }
		},
		{
			"LMABV.",
			new int[1] { 215 }
		},
		{
			"LMACU.",
			new int[1] { 216 }
		},
		{
			"LMADC.",
			new int[1] { 217 }
		},
		{
			"LMADU.",
			new int[1] { 218 }
		},
		{
			"LMAGJ.",
			new int[1] { 219 }
		},
		{
			"LMAGR.",
			new int[1] { 220 }
		},
		{
			"LMAIG.",
			new int[1] { 221 }
		},
		{
			"LMAJQ.",
			new int[1] { 222 }
		},
		{
			"LMAJP.",
			new int[1] { 223 }
		}
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetPlayFabId(string unityItemId, string unityDisplayName, string unityOverrideDisplayName, out string playFabId)
	{
		if (k_v1DisplayNames_to_playFabIds.TryGetValue(unityItemId, out playFabId) || k_v1DisplayNames_to_playFabIds.TryGetValue(unityDisplayName, out playFabId) || k_v1DisplayNames_to_playFabIds.TryGetValue(unityOverrideDisplayName, out playFabId) || k_special.TryGetValue(unityItemId, out playFabId) || k_special.TryGetValue(unityDisplayName, out playFabId) || k_special.TryGetValue(unityOverrideDisplayName, out playFabId) || k_packs.TryGetValue(unityItemId, out playFabId) || k_packs.TryGetValue(unityDisplayName, out playFabId) || k_packs.TryGetValue(unityOverrideDisplayName, out playFabId) || k_oldPacks.TryGetValue(unityItemId, out playFabId) || k_oldPacks.TryGetValue(unityDisplayName, out playFabId) || k_oldPacks.TryGetValue(unityOverrideDisplayName, out playFabId) || k_unused.TryGetValue(unityItemId, out playFabId) || k_unused.TryGetValue(unityDisplayName, out playFabId) || k_unused.TryGetValue(unityOverrideDisplayName, out playFabId))
		{
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetPlayFabId(string unityItemId, out string playFabId, bool logErrors = false)
	{
		if (k_v1DisplayNames_to_playFabIds.TryGetValue(unityItemId, out playFabId) || k_special.TryGetValue(unityItemId, out playFabId) || k_packs.TryGetValue(unityItemId, out playFabId) || k_oldPacks.TryGetValue(unityItemId, out playFabId) || k_unused.TryGetValue(unityItemId, out playFabId))
		{
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetBodyDockAllObjectsIndexes(string playFabId, out int[] bdAllIndexes)
	{
		return _k_playFabId_to_bodyDockPositions_allObjects_indexes.TryGetValue(playFabId, out bdAllIndexes);
	}
}
