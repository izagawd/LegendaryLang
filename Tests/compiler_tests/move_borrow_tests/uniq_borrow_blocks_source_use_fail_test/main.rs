struct Foo['a] {
    kk: &'a mut i32
}
fn main() -> i32 {
    let a = 5;
    let dd = make Foo { kk: &mut a };
    a
}
