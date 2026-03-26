
struct Foo{}
impl Add<Foo> for i32{
    type Output = i32;
    fn add(dd: i32, other: Foo) -> i32{
            dd
        }
    }
fn main() -> i32{
    let a : Foo = Foo{};
    4 + Foo{}
}
