# DevPad — Test Values

Copy-paste these into each tool to verify formatting and highlighting.

---

## JSON

```json
{"user":{"id":1,"name":"Yasin Gulcimen","email":"yasin@example.com","roles":["admin","developer"],"address":{"city":"Istanbul","country":"Turkey","zip":"34000"},"preferences":{"theme":"dark","language":"en","notifications":true},"createdAt":"2024-01-15T10:30:00Z","lastLogin":null,"score":98.5}}
```

**Expected output:** Formatted with indentation, color-coded keys (blue), strings (orange/green), numbers, booleans, null. Tree view should show expandable nodes.

---

## XML

```xml
<?xml version="1.0" encoding="UTF-8"?><catalog><book id="1" available="true"><title>Clean Code</title><author>Robert C. Martin</author><price currency="USD">35.99</price><tags><tag>programming</tag><tag>best-practices</tag></tags></book><book id="2" available="false"><title>The Pragmatic Programmer</title><author>David Thomas</author><price currency="USD">42.00</price><tags><tag>programming</tag><tag>career</tag></tags></book></catalog>
```

**Expected output:** Formatted with indentation, color-coded tags, attributes, values. Tree view should show expandable nodes.

---

## SQL

### Basic SELECT
```sql
select u.id, u.first_name, u.last_name, u.email, u.created_at, d.name as department_name, d.location from users u inner join departments d on u.department_id = d.id where u.active = 1 and u.created_at >= '2024-01-01' order by u.last_name asc, u.first_name asc
```

### Complex with subquery
```sql
select e.id, e.name, e.salary, e.department_id, d.name as dept_name, coalesce(m.name, 'No Manager') as manager_name from employees e inner join departments d on e.department_id = d.id left join employees m on e.manager_id = m.id where e.salary > (select avg(salary) from employees) and e.status = 'active' and e.department_id in (select id from departments where budget > 100000) group by e.id, e.name, e.salary, e.department_id, d.name, m.name order by e.salary desc, e.name asc
```

### INSERT
```sql
insert into users (first_name, last_name, email, phone, department_id, created_at) values ('Yasin', 'Gulcimen', 'yasin@example.com', '+90-555-1234', 3, getdate())
```

### UPDATE
```sql
update employees set first_name = 'John', last_name = 'Doe', email = 'john.doe@example.com', salary = 75000, updated_at = getdate() where id = 42 and status = 'active'
```

**Expected output:** Each SELECT field on its own indented line, JOIN on its own line, ON indented below JOIN, AND/OR indented.

---

## JWT

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Illhc2luIEd1bGNpbWVuIiwiZW1haWwiOiJ5YXNpbkBleGFtcGxlLmNvbSIsInJvbGVzIjpbImFkbWluIiwiZGV2ZWxvcGVyIl0sImlhdCI6MTcwNTMxMjIwMCwiZXhwIjo5OTk5OTk5OTk5fQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

**Expected output:** Header and Payload decoded and displayed. The `exp` field is far in the future so it should show as valid/not expired.

---

## Base64

### Encode this text:
```
Hello, DevPad! This is a test string with special chars: @#$%^&*()
```

### Decode this:
```
SGVsbG8sIERldlBhZCEgVGhpcyBpcyBhIHRlc3Qgc3RyaW5nIHdpdGggc3BlY2lhbCBjaGFyczogQCMkJV4mKigp
```

**Expected output (decode):** `Hello, DevPad! This is a test string with special chars: @#$%^&*()`

### URL-safe Base64 (toggle URL-safe checkbox ON, then decode):
```
SGVsbG8-V29ybGQ_dGVzdA==
```
