struct Foo { x: i32, y: i32 }
fn make() -> Foo { make Foo { x: 1, y: 2 } }
fn main() -> i32 {
    let f = make();
    f.x + f.y
}
