struct Bar['a] { field: &'a i32 }
struct Foo['a] { dd: Bar['a] }
fn main() -> i32 {
    let x: i32 = 10;
    let f = make Foo { dd: make Bar { field: &x } };
    *f.dd.field
}
