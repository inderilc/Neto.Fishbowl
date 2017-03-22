select SO.num 
from SO 
join CUSTOMVARCHAR as CARTCODE on CARTCODE.customfieldid = (select customfield.id from customfield where tableid = 1012013120 and name = 'Shopping Cart Code') and CARTCODE.recordid=SO.id
join CUSTOMVARCHAR as CARTORDER on CARTORDER.customfieldid = (select id from customfield where tableid = 1012013120 and name = 'Shopping Cart Order Number') and CARTORDER.recordid=SO.id
where CARTORDER.info = @scon and CARTCODE.info = @scc