struct Foo {
    val: i32
}
impl Copy for Foo {}
trait GetRef {
    fn get_ref(self: &Self, other: &i32) -> &i32;
}
impl GetRef for Foo {
    fn get_ref(self: &Foo, other: &i32) -> &i32 {
        &self.val
    }
}
fn main() -> i32 {
    let f = make Foo { val : 42 };
    let x = 5;
    let r = f.get_ref(&x);
    *r
}
