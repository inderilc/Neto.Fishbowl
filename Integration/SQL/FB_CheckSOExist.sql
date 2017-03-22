select first 1 so.num 
from so 
join CUSTOMVARCHAR as CartCode on CartCode.customfieldid = (select id from customfield where tableid = 1012013120 and name = 'Shopping Cart Code') and CartCode.recordid=so.id
join CUSTOMVARCHAR as CartOrder on CartOrder.customfieldid = (select id from customfield where tableid = 1012013120 and name = 'Shopping Cart Order Number') and CartOrder.recordid=so.id
where CartOrder.info = @scon and CartCode.info = @scc