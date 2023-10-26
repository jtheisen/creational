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


select p.Type, p.Step, p.StepError, c.Text
from Pages p
join PageContents c on p.lang = c.lang and p.Title = c.Title
where p.Title = 'Template:Taxonomy/life'

select [Type], Title
from Pages
where Title like 'Template:Taxonomy/%' and [Type] <> 3


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
update Pages
set Step = 2, StepError = null
where Type in (2, 3) and Title = 'Template:Taxonomy/life'
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