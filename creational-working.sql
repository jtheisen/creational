select top 1000 c.text
from Pages p
join PageContents c on p.lang = c.lang and p.Title = c.Title
where p.[type] = 3
go

--update p
--set [Step] = 2
--from Pages p
--join PageContents c on p.lang = c.lang and p.Title = c.Title
--where p.[type] = 3

-- Show page garbage
select top 100 *
from Pages
where upper(substring(Title, 1, 1)) <> substring(Title, 1, 1) collate Latin1_General_CS_AI

-- Remove taxo template garbage
delete
from Pages
where Title like 'Template:Taxonomy/%' and upper(substring(Title, 19, 1)) <> substring(Title, 19, 1) collate Latin1_General_CS_AI


select p.Title, c.Title, p.Type, p.Step, p.StepError, t.Taxobox, v.SameAs, c.Text, t.Taxobox
--select t.Taxobox
from Pages p
left join PageContents c on p.lang = c.lang and p.Title = c.Title
left join Taxoboxes t on p.lang = t.lang and p.Title = t.Title
left join TaxoTemplateValues v on p.lang = v.lang and p.Title = v.Title
where p.Title like 'Template:Taxonomy/Bacteroidota-Chlorobiota group'

update c
set [Text] = '{{Don''t edit this line {{{machine code|}}}
|rank=clade
|always_display=no
|link=Core eudicots
|parent=Eudicots
|refs={{Cite journal|author=Angiosperm Phylogeny Group|year=2016|title=An update of the Angiosperm Phylogeny Group classification for the orders and families of flowering plants: APG IV|journal=Botanical Journal of the Linnean Society|volume=181|issue=1|pages=1–20|url=http://onlinelibrary.wiley.com/doi/10.1111/boj.12385/epdf|format=PDF|issn=00244074|doi=10.1111/boj.12385}}
}}
'
from Pages p
join PageContents c on p.lang = c.lang and p.Title = c.Title
where p.Title = 'Template:Taxonomy/Core eudicots'


select [Step], [StepError], [Type], Title
from Pages
where Title like 'Template:Taxonomy/parahoxozoa' and [Type] = 3


select *
from TaxoTemplateValues
where Title = 'Template:Taxonomy/araneae'


select [Key], count(*)
from TaxoboxEntries
group by [Key]
order by count(*) desc

select [Type], [Step], count(*), [StepError]
from Pages
group by [Type], [Step], [StepError]
order by [Type], [Step], [StepError]

-- edit to reparse some set of pages
update p
set Step = 2, StepError = null
from Pages p
join Taxoboxes t on p.lang = t.lang and p.Title = t.Title
where p.[Type] in (2, 3) and p.Step = -2
;

-- remove parsing results for to-be-parsed pages
delete r
from Pages p
join ParsingResults r on p.lang = r.lang and p.Title = r.Title
where Step = 2
;

select top 1000 v.*, e.Value
from TaxoTemplateValues v
join TaxoboxEntries e on v.lang = e.lang and v.Title = e.Title and e.[Key] = 'link'

--merge Taxoboxes t
--using (
--	select p.Lang, p.Title, c.Sha1, c.[Text]
--	from Pages p
--	join PageContents c on p.lang = c.lang and p.Title = c.Title
--	where p.[type] = 3
--) s
--on (t.Lang = s.Lang and t.Title = s.Title)
--when matched then
--	update set Sha1 = s.Sha1, [Taxobox] = s.[Text]
--when not matched then
--	insert (Lang, Title, Sha1, [Taxobox])
--	values (s.Lang, s.Title, s.Sha1, s.[Text]);

update Pages set [Type] = case when [Type] = 2 then 1 when [Type] = 1 then 2 else [Type] end;

select *
from Pages
where title like 'Template:Taxonomy/%' and title like '%Tetrapulmonata%'