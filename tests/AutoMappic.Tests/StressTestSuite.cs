using System;
using System.Collections.Generic;
using System.Linq;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class LargeSource
{
    public string P1 { get; set; } = "1";
    public string P2 { get; set; } = "2";
    public string P3 { get; set; } = "3";
    public string P4 { get; set; } = "4";
    public string P5 { get; set; } = "5";
    public string P6 { get; set; } = "6";
    public string P7 { get; set; } = "7";
    public string P8 { get; set; } = "8";
    public string P9 { get; set; } = "9";
    public string P10 { get; set; } = "10";
    public string P11 { get; set; } = "11";
    public string P12 { get; set; } = "12";
    public string P13 { get; set; } = "13";
    public string P14 { get; set; } = "14";
    public string P15 { get; set; } = "15";
    public string P16 { get; set; } = "16";
    public string P17 { get; set; } = "17";
    public string P18 { get; set; } = "18";
    public string P19 { get; set; } = "19";
    public string P20 { get; set; } = "20";
    public string P21 { get; set; } = "21";
    public string P22 { get; set; } = "22";
    public string P23 { get; set; } = "23";
    public string P24 { get; set; } = "24";
    public string P25 { get; set; } = "25";
    public string P26 { get; set; } = "26";
    public string P27 { get; set; } = "27";
    public string P28 { get; set; } = "28";
    public string P29 { get; set; } = "29";
    public string P30 { get; set; } = "30";
    public string P31 { get; set; } = "31";
    public string P32 { get; set; } = "32";
    public string P33 { get; set; } = "33";
    public string P34 { get; set; } = "34";
    public string P35 { get; set; } = "35";
    public string P36 { get; set; } = "36";
    public string P37 { get; set; } = "37";
    public string P38 { get; set; } = "38";
    public string P39 { get; set; } = "39";
    public string P40 { get; set; } = "40";
    public string P41 { get; set; } = "41";
    public string P42 { get; set; } = "42";
    public string P43 { get; set; } = "43";
    public string P44 { get; set; } = "44";
    public string P45 { get; set; } = "45";
    public string P46 { get; set; } = "46";
    public string P47 { get; set; } = "47";
    public string P48 { get; set; } = "48";
    public string P49 { get; set; } = "49";
    public string P50 { get; set; } = "50";
    public string P51 { get; set; } = "51";
    public string P52 { get; set; } = "52";
    public string P53 { get; set; } = "53";
    public string P54 { get; set; } = "54";
    public string P55 { get; set; } = "55";
    public string P56 { get; set; } = "56";
    public string P57 { get; set; } = "57";
    public string P58 { get; set; } = "58";
    public string P59 { get; set; } = "59";
    public string P60 { get; set; } = "60";
    public string P61 { get; set; } = "61";
    public string P62 { get; set; } = "62";
    public string P63 { get; set; } = "63";
    public string P64 { get; set; } = "64";
    public string P65 { get; set; } = "65";
    public string P66 { get; set; } = "66";
    public string P67 { get; set; } = "67";
    public string P68 { get; set; } = "68";
    public string P69 { get; set; } = "69";
    public string P70 { get; set; } = "70";
    public string P71 { get; set; } = "71";
    public string P72 { get; set; } = "72";
    public string P73 { get; set; } = "73";
    public string P74 { get; set; } = "74";
    public string P75 { get; set; } = "75";
    public string P76 { get; set; } = "76";
    public string P77 { get; set; } = "77";
    public string P78 { get; set; } = "78";
    public string P79 { get; set; } = "79";
    public string P80 { get; set; } = "80";
    public string P81 { get; set; } = "81";
    public string P82 { get; set; } = "82";
    public string P83 { get; set; } = "83";
    public string P84 { get; set; } = "84";
    public string P85 { get; set; } = "85";
    public string P86 { get; set; } = "86";
    public string P87 { get; set; } = "87";
    public string P88 { get; set; } = "88";
    public string P89 { get; set; } = "89";
    public string P90 { get; set; } = "90";
    public string P91 { get; set; } = "91";
    public string P92 { get; set; } = "92";
    public string P93 { get; set; } = "93";
    public string P94 { get; set; } = "94";
    public string P95 { get; set; } = "95";
    public string P96 { get; set; } = "96";
    public string P97 { get; set; } = "97";
    public string P98 { get; set; } = "98";
    public string P99 { get; set; } = "99";
    public string P100 { get; set; } = "100";
}

public class LargeDto
{
    public string P1 { get; set; } = "";
    public string P2 { get; set; } = "";
    public string P3 { get; set; } = "";
    public string P4 { get; set; } = "";
    public string P5 { get; set; } = "";
    public string P6 { get; set; } = "";
    public string P7 { get; set; } = "";
    public string P8 { get; set; } = "";
    public string P9 { get; set; } = "";
    public string P10 { get; set; } = "";
    public string P11 { get; set; } = "";
    public string P12 { get; set; } = "";
    public string P13 { get; set; } = "";
    public string P14 { get; set; } = "";
    public string P15 { get; set; } = "";
    public string P16 { get; set; } = "";
    public string P17 { get; set; } = "";
    public string P18 { get; set; } = "";
    public string P19 { get; set; } = "";
    public string P20 { get; set; } = "";
    public string P21 { get; set; } = "";
    public string P22 { get; set; } = "";
    public string P23 { get; set; } = "";
    public string P24 { get; set; } = "";
    public string P25 { get; set; } = "";
    public string P26 { get; set; } = "";
    public string P27 { get; set; } = "";
    public string P28 { get; set; } = "";
    public string P29 { get; set; } = "";
    public string P30 { get; set; } = "";
    public string P31 { get; set; } = "";
    public string P32 { get; set; } = "";
    public string P33 { get; set; } = "";
    public string P34 { get; set; } = "";
    public string P35 { get; set; } = "";
    public string P36 { get; set; } = "";
    public string P37 { get; set; } = "";
    public string P38 { get; set; } = "";
    public string P39 { get; set; } = "";
    public string P40 { get; set; } = "";
    public string P41 { get; set; } = "";
    public string P42 { get; set; } = "";
    public string P43 { get; set; } = "";
    public string P44 { get; set; } = "";
    public string P45 { get; set; } = "";
    public string P46 { get; set; } = "";
    public string P47 { get; set; } = "";
    public string P48 { get; set; } = "";
    public string P49 { get; set; } = "";
    public string P50 { get; set; } = "";
    public string P51 { get; set; } = "";
    public string P52 { get; set; } = "";
    public string P53 { get; set; } = "";
    public string P54 { get; set; } = "";
    public string P55 { get; set; } = "";
    public string P56 { get; set; } = "";
    public string P57 { get; set; } = "";
    public string P58 { get; set; } = "";
    public string P59 { get; set; } = "";
    public string P60 { get; set; } = "";
    public string P61 { get; set; } = "";
    public string P62 { get; set; } = "";
    public string P63 { get; set; } = "";
    public string P64 { get; set; } = "";
    public string P65 { get; set; } = "";
    public string P66 { get; set; } = "";
    public string P67 { get; set; } = "";
    public string P68 { get; set; } = "";
    public string P69 { get; set; } = "";
    public string P70 { get; set; } = "";
    public string P71 { get; set; } = "";
    public string P72 { get; set; } = "";
    public string P73 { get; set; } = "";
    public string P74 { get; set; } = "";
    public string P75 { get; set; } = "";
    public string P76 { get; set; } = "";
    public string P77 { get; set; } = "";
    public string P78 { get; set; } = "";
    public string P79 { get; set; } = "";
    public string P80 { get; set; } = "";
    public string P81 { get; set; } = "";
    public string P82 { get; set; } = "";
    public string P83 { get; set; } = "";
    public string P84 { get; set; } = "";
    public string P85 { get; set; } = "";
    public string P86 { get; set; } = "";
    public string P87 { get; set; } = "";
    public string P88 { get; set; } = "";
    public string P89 { get; set; } = "";
    public string P90 { get; set; } = "";
    public string P91 { get; set; } = "";
    public string P92 { get; set; } = "";
    public string P93 { get; set; } = "";
    public string P94 { get; set; } = "";
    public string P95 { get; set; } = "";
    public string P96 { get; set; } = "";
    public string P97 { get; set; } = "";
    public string P98 { get; set; } = "";
    public string P99 { get; set; } = "";
    public string P100 { get; set; } = "";
}

public class StressProfile : Profile
{
    public StressProfile()
    {
        CreateMap<LargeSource, LargeDto>();
    }
}

public class StressTestSuite
{
    [Fact]
    [Description("Stress: Verify mapping of a class with 100 properties to ensure performance and correctness.")]
    public void Test_LargeClassMapping_Stress()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StressProfile>());
        var mapper = config.CreateMapper();

        var source = new LargeSource();
        var result = mapper.Map<LargeDto>(source);

        Assert.Equal("1", result.P1);
        Assert.Equal("50", result.P50);
        Assert.Equal("100", result.P100);
    }
}
