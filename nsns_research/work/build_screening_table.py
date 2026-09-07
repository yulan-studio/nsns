import csv,json,re
from pathlib import Path

ROOT=Path(__file__).resolve().parent
items=json.loads((ROOT/'deduplicated_candidates.json').read_text(encoding='utf-8'))

main={
'10.7860/jcdr/2025/73346.20624':'焦虑',
'10.1007/s10803-020-04423-5':'焦虑、内化及行为问题',
'10.3390/ijerph19095471':'焦虑',
'10.1016/j.rasd.2022.102005':'焦虑',
'10.3390/bs14020116':'易激惹、社会退缩及问题行为',
'10.1016/j.explore.2022.12.004':'易激惹、社会退缩及问题行为',
'10.3390/biology11050657':'情绪调节',
'10.1016/j.rasd.2021.101758':'问题行为',
'10.1016/j.rasd.2021.101860':'情感状态',
'10.3389/fpsyt.2022.1027799':'行为及生活质量相关结局',
'10.1177/0031512517743823':'生活质量及情绪反应',
}
secondary={
'10.1016/j.sleep.2021.03.045':'睡眠',
'10.1177/1362361318823910':'睡眠',
'10.1177/13623613211062952':'睡眠及行为功能',
}
multi={
'10.1177/1362361320974841':'瑜伽与第三波认知行为治疗组合，无法分离运动作用',
'10.1177/1539449220912723':'马术环境中的作业治疗，无法分离运动作用',
'10.1294/jes.20.79':'心理教育与骑马组合且样本混合，无法分离运动作用',
'10.3389/fpsyg.2021.588418':'舞蹈/动作心理治疗，无法分离单纯运动作用',
'10.1089/acm.2010.0834':'瑜伽、舞蹈、音乐与放松反应组合，无法分离运动作用',
}
adult={'10.2196/35701':'成人研究','10.1002/smi.1391':'13—27岁混合样本，未提供儿童可分离结果'}
reviews={'10.1007/s40279-021-01545-3':'二次综述，不作为原始研究重复纳入'}
acute={'10.1002/aur.2977':'单次急性运动，不满足至少4周训练'}

def key(x): return (x or '').lower().replace('https://doi.org/','').strip().rstrip('.')
def display_title(x):
    if x.get('title'): return x['title']
    c=x.get('citation','')
    return c[:240]

out=[]
for x in items:
    d=key(x.get('doi'))
    if d in main: decision,reason='纳入主要/相关精神健康证据',f"符合儿童、重复运动训练和预设结局：{main[d]}"
    elif d in secondary: decision,reason='纳入次要结局证据',f"符合儿童和运动训练；仅用于单独的{secondary[d]}综合"
    elif d in multi: decision,reason='排除',multi[d]
    elif d in adult: decision,reason='排除',adult[d]
    elif d in reviews: decision,reason='排除',reviews[d]
    elif d in acute: decision,reason='排除',acute[d]
    else: decision,reason='排除','未报告预先定义的精神健康结局，或只报告运动、认知、社会沟通或核心自闭症特征'
    out.append({
      'title_or_citation':display_title(x),'year':x.get('year',''),'pmid':x.get('pmid',''),'doi':x.get('doi',''),
      'source_reviews':';'.join(x.get('source_reviews',[])),'reported_outcomes':'; '.join(x.get('outcomes',[])),
      'screening_decision':decision,'reason':reason,'verification':'综述纳入表/参考文献交叉核验；最终数据提取须回到原始全文'
    })

# Two highly relevant original studies found in the database search but not represented in the three review tables.
out.extend([
 {'title_or_citation':'Trampoline exercise reduces anxiety and improves motor skills in children with autism spectrum disorder: A randomized controlled trial','year':'2026','pmid':'42116390','doi':'10.1097/MD.0000000000048616','source_reviews':'数据库补充检索','reported_outcomes':'焦虑（SCARED）；HRV','screening_decision':'纳入主要/相关精神健康证据','reason':'符合儿童、重复运动训练、有比较组和焦虑结局','verification':'PMC全文已核验'},
 {'title_or_citation':'Brief Report: Impact of a Physical Exercise Intervention on Emotion Regulation and Behavioral Functioning in Children with Autism Spectrum Disorder','year':'2020','pmid':'32130593','doi':'10.1007/s10803-020-04418-2','source_reviews':'数据库补充检索','reported_outcomes':'情绪调节；行为功能','screening_decision':'纳入主要/相关精神健康证据','reason':'符合儿童、12周慢跑、随机分组和预设精神健康结局','verification':'题名摘要已核验；原始全文待获取'},
])

target=ROOT.parent/'outputs'/'参考文献去重与正式筛选.csv'
with target.open('w',newline='',encoding='utf-8-sig') as f:
    w=csv.DictWriter(f,fieldnames=list(out[0]));w.writeheader();w.writerows(out)
from collections import Counter
print(json.dumps({'total':len(out),'decisions':Counter(x['screening_decision'] for x in out)},ensure_ascii=False,default=dict))
