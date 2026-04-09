struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
    fn get_uniq(self: &uniq Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 5 };
    let r: &uniq Foo = &uniq f;
    f.get();
    r.get_uniq()
}
