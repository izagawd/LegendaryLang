struct Foo { val: i32 }
impl Foo {
    fn get(self: Self) -> &i32 { &self.val }
}
fn main() -> i32 { 0 }
