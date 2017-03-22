select * from (

select product.num, COALESCE( IF( SUM(QTYINVENTORYTOTALS.QTYONHAND) - SUM(QTYINVENTORYTOTALS.QTYALLOCATED) < 0, 0, SUM(QTYINVENTORYTOTALS.QTYONHAND) - SUM(QTYINVENTORYTOTALS.QTYALLOCATED) ) ,0) as QTY
from PART
    join product on product.partid = part.id
    left join QTYINVENTORYTOTALS on QTYINVENTORYTOTALS.PARTID = part.id
where part.typeid = 10 
group by 1 

union all

select product.num, LEAST(SUM(tag.QTY)-SUM(tag.QTYCOMMITTED),0) as QTY
from product
	join kititem on kititem.kitproductid = product.id
    join product as kp on kp.id = kititem.productid
    join tag on tag.partid = kp.partid
where product.kitflag = 1
group by 1

) as k