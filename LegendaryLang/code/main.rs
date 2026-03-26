
struct Foo{}
fn main() -> i32{
    let a = Foo{};
    {
        let a = Foo{};
        let c = a;
        
        }
    let c = a;
    5
}
