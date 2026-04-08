struct Foo { x: i32, y: i32 }
fn main() -> i32 {
    let f = make Foo { x: 1, y: 2 };
    f.x + f.y
}
