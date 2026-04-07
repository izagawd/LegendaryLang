enum Dir { Up, Down, Left, Right }
use Dir.Left;
use Dir.Right;

fn main() -> i32 {
    let d = Dir.Left;
    match d {
        Left => 1,
        Right => 2,
        _ => 0
    }
}
