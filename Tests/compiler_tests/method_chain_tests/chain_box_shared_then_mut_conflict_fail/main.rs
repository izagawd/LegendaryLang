// b.get_ref() returns &i32 (shared borrow). b.set(99) takes &mut Self while r is live.
struct Foo { val: i32 }
impl Foo {
    fn get_ref(self: &Self) -> &i32 { &self.val }
    fn set(self: &mut Self, v: i32) { self.val = v; }
}
fn main() -> i32 {
    let b = Box.New(make Foo { val: 42 });
    let r: &i32 = b.get_ref();
    b.set(99);
    *r
}
