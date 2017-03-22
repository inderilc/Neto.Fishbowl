select * from (

select product.num, COALESCE( IIF( SUM(QTYINVENTORYTOTALS.QTYONHAND) - SUM(QTYINVENTORYTOTALS.QTYALLOCATED) < 0, 0, SUM(QTYINVENTORYTOTALS.QTYONHAND) - SUM(QTYINVENTORYTOTALS.QTYALLOCATED) ) ,0) as QTY
from PART
    join product on product.partid = part.id
    left join QTYINVENTORYTOTALS on QTYINVENTORYTOTALS.PARTID = part.id
where part.typeid = 10 
group by 1 

union all

select product.num, MIN ( COALESCE( (select SUM(tag.qty)-SUM(tag.QTYCOMMITTED) from tag where partid = kp.partid),0) ) as QTY
from product
	join kititem on kititem.kitproductid = product.id
    join product as kp on kp.id = kititem.productid    
where product.kitflag = 1
group by 1

) as k