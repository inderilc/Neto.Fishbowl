SELECT
	ship.num SNUM,
	so.CUSTOMERPO as CPO,
	bcaid.info as BCAID,
	so.num as ORDERNUM,
	shipcarton.TRACKINGNUM,
	c.NAME as CarrierName,
	s.NAME as ShipService,
	soitem.CUSTOMERPARTNUM as ITEMID,
	shipitem.QTYSHIPPED

from SHIP
    join so on so.id = ship.SOID
	left join customvarchar as scc on scc.customfieldid = (select id from customfield where name = 'Shopping Cart Code') and scc.recordid = so.id
	join CARRIER c on c.ID = ship.CARRIERID
    join shipcarton on shipcarton.shipid = ship.ID
    left join CARRIERSERVICE as s on s.id = ship.CARRIERSERVICEID
	join shipitem on shipitem.shipcartonid = shipcarton.id
	join soitem on soitem.id = shipitem.soitemid
where ship.DATESHIPPED > @dte  
and scc.info = @scc
