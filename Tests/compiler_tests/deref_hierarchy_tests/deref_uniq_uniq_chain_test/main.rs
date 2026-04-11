struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn do_shit(self: &mut Self) -> i32 { self.val }
}
fn main() -> i32 {
    let dd = make Foo { val: 42 };
    let idk = &mut dd;
    let gotten: &mut &mut Foo = &mut idk;
    gotten.do_shit()
}
