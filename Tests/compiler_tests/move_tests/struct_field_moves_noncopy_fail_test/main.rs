use Std.Mem.ManuallyDrop;

fn main() -> i32 {
    let made = Box.New(4);
    let manually = ManuallyDrop.New(made);
    let foo = made;
    5
}
