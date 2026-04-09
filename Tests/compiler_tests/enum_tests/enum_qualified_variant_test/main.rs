enum Dir { Up, Down, Left, Right }

fn main() -> i32 {
    let d = Dir.Left;
    match d {
        Dir.Up => 1,
        Dir.Down => 2,
        Dir.Left => 3,
        Dir.Right => 4
    }
}
