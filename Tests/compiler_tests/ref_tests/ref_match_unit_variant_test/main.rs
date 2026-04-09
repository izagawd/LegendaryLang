enum Dir { North, South, East, West }

fn to_num(d: &Dir) -> i32 {
    match d {
        Dir.North => 1,
        Dir.South => 2,
        Dir.East => 3,
        Dir.West => 4
    }
}

fn main() -> i32 {
    let d = Dir.East;
    to_num(&d)
}
