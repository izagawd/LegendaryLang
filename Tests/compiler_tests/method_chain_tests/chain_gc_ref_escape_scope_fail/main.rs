// GcMut in inner block, method returns &i32. Reference escapes → dangling.
struct Foo { val: i32 }
impl Foo { fn get_ref(self: &Self) -> &i32 { &self.val } }
fn main() -> i32 {
    let r: &i32 = {
        let b = GcMut.New(make Foo { val: 42 });
        b.get_ref()
    };
    *r
}
