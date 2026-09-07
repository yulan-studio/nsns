import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "../../outputs";
const previewDir = "../spreadsheet_previews";
await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const studies = [
  ["Dhingra 2025","RCT（单盲）","印度","38","7–13岁；轻至中度","19","19","有氧训练","8周；3次/周；30分钟；最大心率65%–85%","自闭症知识讲座；两组继续常规康复","SCARED","焦虑","运动组44(42–50)→42(41–46)；对照47(45–52)→51(44–53)","干预后组间p=0.004；未报告组间效应量/95%CI","38/38完成","训练日记记录事件，但结果未报告数量","核心焦虑证据","10.7860/JCDR/2025/73346.20624","","https://www.jcdr.net/article_fulltext.aspx?id=20624&issn=0973-709x&issue=2&page=SC06+-+SC11&volume=19&year=2025"],
  ["Carey 2022","单组前后研究","爱尔兰","47","5–18岁；均为男孩","47","0","学校运动：基本动作技能与小组游戏","16周；3次/周；60分钟","无对照","ASC-ASD（家长/教师）","焦虑","教师报告改善；家长报告无显著变化","无对照，不能估计运动相对效应","家长与教师完整三时间点各20名；作者报告结局流失42.55%","未报告","证据图谱/叙述性综合","10.3390/ijerph19095471","35564866","https://pmc.ncbi.nlm.nih.gov/articles/PMC9104305/"],
  ["Ju 2024","RCT","中国","17","均为儿童；均值11.11与12.75岁","9","8","团体瑜伽：呼吸、热身、体式强化、放松","8周；3次/周；45–50分钟；4周延迟随访","日常课程活动","ABC（教师）","问题行为：易激惹、社会退缩等","8周ABC总分77.89±10.57 vs 95.88±7.77","Bonferroni p=0.046；论文报告d=1.847","以17名报告；是否存在结局缺失未清楚说明","未系统报告","相关精神健康/行为证据","10.3390/bs14020116","38392469","https://pmc.ncbi.nlm.nih.gov/articles/PMC10886297/"],
  ["Marzouki 2022","三组RCT","突尼斯","28","6–7岁","10+10","8","技术型或游戏型水中训练","8周；2次/周；50分钟","常规体育活动","ERC（家长）","情绪调节；情绪不稳定/负性","两个ERC结局的组别×时间交互均不显著","情绪调节F(2,19)=0.826,p=0.453；负性F(2,19)=1.641,p=0.220","最终22/28；6名健康问题退出并排除分析（21.4%）","作者称退出健康问题非训练所致；无逐项事件表","主要叙述性综合（阴性比较结果）","10.3390/biology11050657","35625385","https://pmc.ncbi.nlm.nih.gov/articles/PMC9138228/"],
  ["Toscano 2022","非随机对照研究","巴西","229","2.3–17.3岁；支持需要等级1/2/3分别143/63/23","127","62+40","个体化协调、力量和平衡训练","48周；2次/周；40分钟；共96次","同机构62名及另一机构40名；均非随机","ATA（家长信息）","反应性、注意、刻板行为、睡眠等","报告运动组反应性等条目随时间改善","贝叶斯多层模型；非随机比较","只纳入出席率≥90%者；未按随机起点分析","未见系统事件表","相关行为支持性证据","10.3389/fpsyt.2022.1027799","36620673","https://pmc.ncbi.nlm.nih.gov/articles/PMC9813515/"],
  ["Luo 2026","随机主动对照试验","中国","50","9–14岁；轻至中度","25","25","蹦床运动","8周；3次/周；30分钟","弹力带训练","SCARED（家长）+ HRV","焦虑","SCARED变化中位数约−9 vs 0","FDR校正p<0.001；Cliff's delta=−0.92","50/50完成；依从性约85% vs 82%","主动监测；两组均0例","核心焦虑证据","10.1097/MD.0000000000048616","42116390","https://pmc.ncbi.nlm.nih.gov/articles/PMC13166828/"]
];

const risk = [
  ["Dhingra 2025","RoB 2（初评）","有些担忧","有些担忧","有些担忧","低","有些担忧","有些担忧","随机方法有描述，但分配隐藏信息不足；参与者不能盲法；未见预注册分析计划；全员完成降低缺失偏倚"],
  ["Ju 2024","RoB 2（初评）","有些担忧","高","有些担忧","有些担忧","高","高","极小样本；教师同时知晓分组并评定主观结局；结局/分析选择报告不足；效应量与校正说明需复核"],
  ["Marzouki 2022","RoB 2（初评）","有些担忧","有些担忧","高","有些担忧","有些担忧","高","随机后6/28被排除且未作意向治疗；退出比例高；主观家长结局；主要交互结果为阴性"],
  ["Luo 2026","RoB 2（初评）","低","有些担忧","低","有些担忧","有些担忧","有些担忧","随机主动对照、完整随访和主动不良事件监测较好；家长主观结局且难以盲法；单中心小样本"],
  ["Carey 2022","ROBINS-I思路（初评）","严重","严重","严重","有些担忧","严重","严重","无对照单组；结局完整数据仅约20/47；自选参与及共同干预难排除；只能描述关联"],
  ["Toscano 2022","ROBINS-I思路（初评）","严重","严重","中等","有些担忧","严重","严重","家庭自行选择组别，选择偏倚和残余混杂突出；虽使用多层调整，仍不能等同随机试验"]
];

const wb = Workbook.create();
const summary = wb.worksheets.add("阶段总结");
const data = wb.worksheets.add("统一提取");
const rob = wb.worksheets.add("偏倚风险初评");
const code = wb.worksheets.add("字段说明");

summary.showGridLines = false;
summary.getRange("A1:H1").merge();
summary.getRange("A1").values = [["运动训练与自闭症儿童精神健康：首批6篇证据"]];
summary.getRange("A1:H1").format = { fill: "#1F4E78", font: { bold: true, color: "#FFFFFF", size: 16 }, rowHeight: 30 };
summary.getRange("A3:B7").values = [["指标","数量"],["全文已核对",6],["核心焦虑证据",2],["相关行为/情绪证据",3],["仅证据图谱",1]];
summary.getRange("A3:B3").format = { fill: "#D9EAF7", font: { bold: true, color: "#17365D" } };
summary.getRange("A3:B7").format.borders = { preset: "outside", style: "thin", color: "#9EADBA" };
summary.getRange("D3:H3").merge(); summary.getRange("D3").values = [["阶段判断"]];
summary.getRange("D4:H7").merge(); summary.getRange("D4").values = [["当前不宜计算一个“精神健康总效应”。SCARED、ASC-ASD、ABC、ERC和ATA测量的是不同构念；研究设计也包括随机试验、非随机研究和无对照单组研究。可先对焦虑、情绪调节、问题行为分层叙述。焦虑Meta分析至少还需取得其余焦虑研究全文并核对可计算数据。"]];
summary.getRange("D3:H3").format = { fill: "#D9EAF7", font: { bold: true, color: "#17365D" } };
summary.getRange("D4:H7").format = { fill: "#F3F7FA", wrapText: true, verticalAlignment: "top" };
summary.getRange("A9:H9").merge(); summary.getRange("A9").values = [["证据方向（仅表示本研究报告，不代表确定因果结论）"]];
summary.getRange("A9:H9").format = { fill: "#E2F0D9", font: { bold: true, color: "#375623" } };
summary.getRange("A10:C16").values = [["研究","方向","解释"],["Dhingra 2025","倾向有益","焦虑量表组间差异显著，但效应量/CI不足"],["Carey 2022","结果不一致","教师阳性、家长不显著；无对照"],["Ju 2024","倾向有益","问题行为总分、易激惹和社会退缩改善；高偏倚风险"],["Marzouki 2022","未显示组间优势","情绪调节两个交互均不显著"],["Toscano 2022","倾向有益但不确定","非随机且结局并非专门焦虑/抑郁量表"],["Luo 2026","倾向有益","焦虑变化较主动对照更大；小型单中心试验"]];
summary.getRange("A10:C10").format = { fill: "#4472C4", font: { bold: true, color: "#FFFFFF" } };
summary.getRange("A10:C16").format.wrapText = true;

const headers = ["研究","设计","国家","入组N","人群","干预N","对照N","运动类型","剂量","对照","量表","结局构念","数值结果","效应/检验","缺失数据","不良事件","证据层级","DOI","PMID","全文来源"];
data.getRangeByIndexes(0,0,1,headers.length).values = [headers];
data.getRangeByIndexes(1,0,studies.length,headers.length).values = studies;
data.tables.add(`A1:T${studies.length+1}`, true, "ExtractionTable").style = "TableStyleMedium2";
data.freezePanes.freezeRows(1); data.freezePanes.freezeColumns(1); data.showGridLines = false;
data.getRange(`A1:T${studies.length+1}`).format.wrapText = true;

const robHeaders = ["研究","工具","随机/混杂","偏离干预","缺失结局","结局测量","选择性报告","总体判断","依据"];
rob.getRangeByIndexes(0,0,1,robHeaders.length).values = [robHeaders];
rob.getRangeByIndexes(1,0,risk.length,robHeaders.length).values = risk;
rob.tables.add(`A1:I${risk.length+1}`, true, "RiskTable").style = "TableStyleMedium4";
rob.freezePanes.freezeRows(1); rob.showGridLines = false; rob.getRange(`A1:I${risk.length+1}`).format.wrapText = true;

code.getRange("A1:C12").values = [
  ["字段/术语","含义","使用规则"],
  ["核心焦虑证据","使用专门焦虑量表并有可比较对照","与问题行为分开综合"],
  ["相关精神健康/行为证据","情绪调节、易激惹、退缩、内化/外化等","不自动等同焦虑或抑郁"],
  ["未报告","论文没有提供所需信息","不能改写为0或无事件"],
  ["RoB 2（初评）","随机试验偏倚风险框架","本表为第一位评审者初评"],
  ["ROBINS-I思路（初评）","非随机干预研究偏倚框架","本表未替代正式逐信号问题评审"],
  ["低","现有信息未提示重要偏倚","不表示研究完美"],
  ["有些担忧","存在信息不足或潜在问题","敏感性分析时需注意"],
  ["高/严重","可能实质影响效应估计","不宜作为强因果证据"],
  ["直接证据","原始全文直接报告","可在全文表格/方法中核验"],
  ["AI判断","根据方法学规则作出的归类","必须与原文事实分开"],
  ["版本说明","更新日期2026-09-02","加入其余全文后必须更新"]
];
code.getRange("A1:C1").format = { fill: "#1F4E78", font: { bold: true, color: "#FFFFFF" } };
code.getRange("A1:C12").format.wrapText = true; code.showGridLines = false; code.freezePanes.freezeRows(1);

for (const [sheet, widths] of [[summary,[22,12,42,18,18,18,18,18]],[data,[18,16,12,12,20,11,11,20,28,24,20,22,34,30,28,28,22,24,14,42]],[rob,[18,22,16,16,16,16,16,16,48]],[code,[24,42,42]]]) {
  widths.forEach((w,i)=>sheet.getRangeByIndexes(0,i,Math.max(1,sheet.getUsedRange().rowCount),1).format.columnWidth=w);
  sheet.getUsedRange().format.verticalAlignment = "top";
  sheet.getUsedRange().format.autofitRows();
}

for (const name of ["阶段总结","统一提取","偏倚风险初评","字段说明"]) {
  const preview = await wb.render({ sheetName: name, autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(`${previewDir}/${name}.png`, new Uint8Array(await preview.arrayBuffer()));
}

const inspection = await wb.inspect({ kind: "table", range: "阶段总结!A1:H16", include: "values,formulas", tableMaxRows: 20, tableMaxCols: 10 });
console.log(inspection.ndjson);
const errors = await wb.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 100 }, summary: "formula error scan" });
console.log(errors.ndjson);

const xlsx = await SpreadsheetFile.exportXlsx(wb);
await xlsx.save(`${outputDir}/6篇研究_统一数据提取与偏倚风险初评.xlsx`);
