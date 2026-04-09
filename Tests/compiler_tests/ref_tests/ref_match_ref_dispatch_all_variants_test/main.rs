enum Dir { N, S, E, W }

fn to_dx(d: &Dir) -> i32 {
    match d {
        Dir.N => 0,
        Dir.S => 0,
        Dir.E => 1,
        Dir.W => 0 - 1
    }
}

fn to_dy(d: &Dir) -> i32 {
    match d {
        Dir.N => 1,
        Dir.S => 0 - 1,
        Dir.E => 0,
        Dir.W => 0
    }
}

fn main() -> i32 {
    let d = Dir.E;
    to_dx(&d) + to_dy(&d)
}
