struct Foo['a] {
    kk: &'a uniq i32
}
fn main() -> i32 {
    let a = 5;
    let dd = make Foo { kk: &uniq a };
    a
}
