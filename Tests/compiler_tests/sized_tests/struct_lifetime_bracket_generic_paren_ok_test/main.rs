struct Foo['a](T:! type) { field: &'a T }
fn main() -> i32 {
    let x: i32 = 42;
    let f = make Foo { field: &x };
    *f.field
}
