enum Color { Red, Green, Blue }
use pkg.Color.Red;

fn main() -> i32 {
    match Color.Red {
        Red => 1,
        _ => 0
    }
}
