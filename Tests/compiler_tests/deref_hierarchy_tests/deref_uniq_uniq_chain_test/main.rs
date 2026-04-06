struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn do_shit(self: &uniq Self) -> i32 { self.val }
}
fn main() -> i32 {
    let dd = make Foo { val: 42 };
    let idk = &uniq dd;
    let gotten: &uniq &uniq Foo = &uniq idk;
    gotten.do_shit()
}
