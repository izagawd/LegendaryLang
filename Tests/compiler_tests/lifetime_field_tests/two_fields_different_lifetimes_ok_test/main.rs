struct Bar['a] { field: &'a i32 }
struct Foo['a, 'b] { dd: Bar['a], other: Bar['b] }
fn main() -> i32 {
    let x: i32 = 3;
    let y: i32 = 4;
    let f = make Foo { dd: make Bar { field: &x }, other: make Bar { field: &y } };
    *f.dd.field + *f.other.field
}
