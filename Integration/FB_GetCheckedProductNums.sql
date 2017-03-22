select
product.num as NUM
from product
join CUSTOMINTEGER as checked on checked.customfieldid = (select id from customfield where tableid = 97022306 and name like @storename) and checked.recordid=product.id