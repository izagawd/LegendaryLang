struct Bar['a] { field: &'a i32 }
struct Foo['a] { dd: Bar['a], other: Bar['a] }
fn main() -> i32 {
    let x: i32 = 3;
    let f = make Foo { dd: make Bar { field: &x }, other: make Bar { field: &x } };
    *f.dd.field + *f.other.field
}
