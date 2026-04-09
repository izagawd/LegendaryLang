// make Foo{val:42}.get_ref() in inner block. Temporary spilled to block scope.
// Ref escapes block → dangling.
struct Foo { val: i32 }
impl Foo { fn get_ref(self: &Self) -> &i32 { &self.val } }
fn main() -> i32 {
    let r: &i32 = {
        make Foo { val: 42 }.get_ref()
    };
    *r
}
